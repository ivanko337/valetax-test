using Commissions.Infrastructure.Processing.Models;
using Microsoft.Extensions.Options;

namespace Commissions.Infrastructure.Processing;

public sealed class ProfitEventWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ProfitEventProcessingOptions> options,
    ILogger<ProfitEventWorker> logger)
    : BackgroundService
{
    private readonly TimeSpan _pollingInterval = options.Value.PollingInterval;
    private readonly int _maxDegreeOfParallelism = options.Value.MaxDegreeOfParallelism;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItems = await ClaimBatchAsync(stoppingToken);

                if (workItems.Count == 0)
                {
                    await Task.Delay(_pollingInterval, stoppingToken);
                    continue;
                }

                await Parallel.ForEachAsync(
                    workItems,
                    new ParallelOptions
                    {
                        CancellationToken = stoppingToken,
                        MaxDegreeOfParallelism = _maxDegreeOfParallelism
                    },
                    ProcessAsync);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Profit event worker failed");
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }
    }

    private async Task<IReadOnlyList<ProfitEventWorkItem>> ClaimBatchAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var claimer = scope.ServiceProvider
            .GetRequiredService<ProfitEventBatchClaimer>();

        return await claimer.ClaimBatchAsync(cancellationToken);
    }

    private async ValueTask ProcessAsync(
        ProfitEventWorkItem workItem,
        CancellationToken cancellationToken)
    {
        try
        {
            // DbContext is not thread-safe. Every parallel event gets an independent
            // dependency-injection scope, DbContext, transaction, and outbox writer.
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider
                .GetRequiredService<ProfitEventProcessor>();

            await processor.ProcessAsync(workItem, cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The persisted claim will become eligible again after ClaimTimeout.
            logger.LogError(
                exception,
                "Unhandled failure while processing profit event {ExternalEventId}",
                workItem.ExternalEventId);
        }
    }
}

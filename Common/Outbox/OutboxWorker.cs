using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Common.Outbox;

public sealed class OutboxWorker<TDbContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxWorker<TDbContext>> logger)
    : BackgroundService
    where TDbContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<OutboxBatchProcessor<TDbContext>>();

                var foundMessages = await processor
                    .ProcessAsync(stoppingToken)
                    .ConfigureAwait(false);

                if (!foundMessages)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox worker failed");

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken)
                    .ConfigureAwait(false);
            }
        }
    }
}

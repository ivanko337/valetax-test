using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Kafka;

internal sealed class KafkaConsumerHostedService<THandler>(
    IKafkaConsumer consumer,
    IServiceScopeFactory scopeFactory,
    IReadOnlyCollection<string> topics)
    : BackgroundService
    where THandler : class, IKafkaMessageHandler
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return consumer.ConsumeAsync(
            topics,
            HandleMessageAsync,
            stoppingToken);
    }

    private async Task HandleMessageAsync(
        string message,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        await handler
            .HandleAsync(message, cancellationToken)
            .ConfigureAwait(false);
    }
}

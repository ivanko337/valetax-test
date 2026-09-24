using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Kafka;

public static class KafkaHostExtensions
{
    public static Task InitializeKafkaTopicsAsync(
        this IHost host,
        IEnumerable<string> topics,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        return host.Services
            .GetRequiredService<KafkaTopicInitializer>()
            .InitializeAsync(topics, cancellationToken);
    }
}

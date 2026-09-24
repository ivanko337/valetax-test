namespace Common.Kafka;

public interface IKafkaPublisher : IDisposable
{
    Task PublishAsync(
        string topic,
        string message,
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        string topic,
        string message,
        string? key,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken = default);
}

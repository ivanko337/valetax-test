namespace Common.Kafka;

public interface IKafkaConsumer : IDisposable
{
    Task ConsumeAsync(
        IEnumerable<string> topics,
        Func<string, CancellationToken, Task> messageHandler,
        CancellationToken cancellationToken = default);
}

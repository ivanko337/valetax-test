namespace Common.Kafka;

public interface IKafkaMessageHandler
{
    Task HandleAsync(
        string message,
        CancellationToken cancellationToken);
}

namespace Common.Outbox;

public interface IMessageTransport
{
    Task PublishAsync(
        Guid messageId,
        string topic,
        string? key,
        string typeName,
        string payload,
        string? traceParent,
        string? traceState,
        CancellationToken cancellationToken);
}

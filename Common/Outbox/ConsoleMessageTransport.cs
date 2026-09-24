namespace Common.Outbox;

public sealed class ConsoleMessageTransport : IMessageTransport
{
    public Task PublishAsync(
        Guid messageId,
        string topic,
        string? key,
        string typeName,
        string payload,
        string? traceParent,
        string? traceState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Console.WriteLine(
            $"Published {messageId} to {topic}; key={key}; " +
            $"type={typeName}; payload={payload}");

        return Task.CompletedTask;
    }
}

using Common.Kafka;

namespace Common.Outbox;

public sealed class KafkaMessageTransport(IKafkaPublisher publisher) : IMessageTransport
{
    public const string MessageIdHeaderName = "outbox-message-id";

    public const string MessageTypeHeaderName = "outbox-message-type";

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
        var headers = new Dictionary<string, string>
        {
            [MessageIdHeaderName] = messageId.ToString(),
            [MessageTypeHeaderName] = typeName
        };

        if (traceParent is not null)
        {
            headers[KafkaTraceContext.TraceParentHeaderName] = traceParent;
        }

        if (traceState is not null)
        {
            headers[KafkaTraceContext.TraceStateHeaderName] = traceState;
        }

        return publisher.PublishAsync(
            topic,
            payload,
            key,
            headers,
            cancellationToken);
    }
}

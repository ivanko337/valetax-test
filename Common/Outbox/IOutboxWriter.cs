namespace Common.Outbox;

public interface IOutboxWriter
{
    Guid Add<TMessage>(
        string topic,
        TMessage message,
        string? key = null);
}

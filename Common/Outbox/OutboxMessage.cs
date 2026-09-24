namespace Common.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Topic { get; set; } = null!;

    public string? MessageKey { get; set; }

    public string TypeName { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public string? TraceParent { get; set; }

    public string? TraceState { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public string? LastError { get; set; }
}

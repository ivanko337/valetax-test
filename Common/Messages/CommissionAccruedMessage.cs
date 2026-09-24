namespace Common.Messages;

public sealed record CommissionAccruedMessage(
    Guid CommissionId,
    Guid ExternalEventId,
    Guid UserExternalId,
    long AmountCents,
    int Level,
    int SchemaVersion,
    DateTimeOffset AccruedAt);

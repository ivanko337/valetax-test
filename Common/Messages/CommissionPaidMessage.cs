namespace Common.Messages;

public sealed record CommissionPaidMessage(
    Guid CommissionId,
    Guid UserExternalId,
    long AmountCents,
    DateTimeOffset PaidAt);

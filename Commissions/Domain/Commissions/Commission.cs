namespace Commissions.Domain.Commissions;

public sealed class Commission(
    Guid id,
    Guid externalEventId,
    Guid beneficiaryId,
    long amountCents,
    int schemaVersion,
    int level = 1)
{
    public Guid Id { get; private set; } = id;

    public Guid ExternalEventId { get; private set; } = externalEventId;

    public Guid BeneficiaryId { get; private set; } = beneficiaryId;

    public int Level { get; private set; } = level;

    public long AmountCents { get; private set; } = amountCents;

    public int SchemaVersion { get; private set; } = schemaVersion;

    public CommissionPaymentStatus PaymentStatus { get; private set; } = CommissionPaymentStatus.Pending;

    public DateTimeOffset? PaidAt { get; private set; }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        PaymentStatus = CommissionPaymentStatus.Paid;
        PaidAt = paidAt;
    }
}

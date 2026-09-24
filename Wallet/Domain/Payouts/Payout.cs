namespace Wallet.Domain.Payouts;

public sealed class Payout(
    Guid commissionId,
    Guid userExternalId,
    long amountCents,
    DateTimeOffset paidAt)
{
    public Guid CommissionId { get; private set; } = commissionId;

    public Guid UserExternalId { get; private set; } = userExternalId;

    public long AmountCents { get; private set; } = amountCents;

    public DateTimeOffset PaidAt { get; private set; } = paidAt;
}

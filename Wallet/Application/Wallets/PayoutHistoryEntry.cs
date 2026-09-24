namespace Wallet.Application.Wallets;

public sealed record PayoutHistoryEntry(
    Guid CommissionId,
    long AmountCents,
    DateTimeOffset PaidAt);

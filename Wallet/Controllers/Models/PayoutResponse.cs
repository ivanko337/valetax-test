using Wallet.Application.Wallets;

namespace Wallet.Controllers.Models;

public sealed record PayoutResponse(
    Guid CommissionId,
    long AmountCents,
    DateTimeOffset PaidAt)
{
    public static PayoutResponse From(PayoutHistoryEntry payout)
    {
        return new PayoutResponse(
            payout.CommissionId,
            payout.AmountCents,
            payout.PaidAt);
    }
}

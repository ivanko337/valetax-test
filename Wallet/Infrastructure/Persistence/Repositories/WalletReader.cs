using Microsoft.EntityFrameworkCore;
using Wallet.Application.Wallets;

namespace Wallet.Infrastructure.Persistence.Repositories;

public sealed class WalletReader(WalletDbContext dbContext) : IWalletReader
{
    public Task<WalletBalance?> GetBalanceAsync(
        Guid userExternalId,
        CancellationToken cancellationToken)
    {
        return dbContext.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.UserExternalId == userExternalId)
            .Select(wallet => new WalletBalance(
                wallet.UserExternalId,
                wallet.BalanceCents))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayoutHistoryEntry>?> GetPayoutHistoryAsync(
        Guid userExternalId,
        CancellationToken cancellationToken)
    {
        var walletExists = await dbContext.Wallets
            .AsNoTracking()
            .AnyAsync(
                wallet => wallet.UserExternalId == userExternalId,
                cancellationToken);

        if (!walletExists)
        {
            return null;
        }

        var payouts = await dbContext.Payouts
            .AsNoTracking()
            .Where(payout => payout.UserExternalId == userExternalId)
            .Select(payout => new PayoutHistoryEntry(
                payout.CommissionId,
                payout.AmountCents,
                payout.PaidAt))
            .ToArrayAsync(cancellationToken);

        return payouts
            .OrderByDescending(payout => payout.PaidAt)
            .ThenBy(payout => payout.CommissionId)
            .ToArray();
    }
}

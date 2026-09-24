namespace Wallet.Application.Wallets;

public interface IWalletReader
{
    Task<WalletBalance?> GetBalanceAsync(
        Guid userExternalId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PayoutHistoryEntry>?> GetPayoutHistoryAsync(
        Guid userExternalId,
        CancellationToken cancellationToken);
}

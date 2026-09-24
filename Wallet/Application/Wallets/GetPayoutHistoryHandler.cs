using Common.Application;

namespace Wallet.Application.Wallets;

public sealed class GetPayoutHistoryHandler(IWalletReader walletReader)
    : IQueryHandler<GetPayoutHistoryQuery, IReadOnlyList<PayoutHistoryEntry>?>
{
    public Task<IReadOnlyList<PayoutHistoryEntry>?> HandleAsync(
        GetPayoutHistoryQuery query,
        CancellationToken cancellationToken)
    {
        return walletReader.GetPayoutHistoryAsync(
            query.UserExternalId,
            cancellationToken);
    }
}

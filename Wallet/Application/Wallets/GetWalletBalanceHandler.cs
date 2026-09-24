using Common.Application;

namespace Wallet.Application.Wallets;

public sealed class GetWalletBalanceHandler(IWalletReader walletReader)
    : IQueryHandler<GetWalletBalanceQuery, WalletBalance?>
{
    public Task<WalletBalance?> HandleAsync(
        GetWalletBalanceQuery query,
        CancellationToken cancellationToken)
    {
        return walletReader.GetBalanceAsync(
            query.UserExternalId,
            cancellationToken);
    }
}

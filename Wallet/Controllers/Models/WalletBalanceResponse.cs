using Wallet.Application.Wallets;

namespace Wallet.Controllers.Models;

public sealed record WalletBalanceResponse(
    Guid UserExternalId,
    long BalanceCents)
{
    public static WalletBalanceResponse From(WalletBalance balance)
    {
        return new WalletBalanceResponse(
            balance.UserExternalId,
            balance.BalanceCents);
    }
}

namespace Wallet.Application.Wallets;

public interface IWalletRepository
{
    Task<bool> TryAddAsync(
        Domain.Wallets.Wallet wallet,
        CancellationToken cancellationToken);
}

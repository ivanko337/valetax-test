namespace Wallet.Application.Wallets;

public sealed record WalletBalance(
    Guid UserExternalId,
    long BalanceCents);

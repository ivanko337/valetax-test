namespace Wallet.Application.Wallets;

public sealed record CreateWalletCommand(
    Guid UserExternalId,
    DateTimeOffset CreatedAt);

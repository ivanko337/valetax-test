using Common.Application;

namespace Wallet.Application.Wallets;

public sealed class CreateWalletHandler(
    IWalletRepository walletRepository,
    ILogger<CreateWalletHandler> logger)
    : ICommandHandler<CreateWalletCommand, CreateWalletResult>
{
    public async Task<CreateWalletResult> HandleAsync(
        CreateWalletCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserExternalId == Guid.Empty)
        {
            throw new ArgumentException(
                "User external ID must not be empty.",
                nameof(command));
        }

        var wallet = new Domain.Wallets.Wallet(
            command.UserExternalId,
            balanceCents: 0,
            command.CreatedAt);

        var wasAdded = await walletRepository.TryAddAsync(
            wallet,
            cancellationToken);

        if (wasAdded)
        {
            logger.LogInformation(
                "Created wallet for user {UserExternalId}",
                wallet.UserExternalId);
            return CreateWalletResult.Created;
        }

        logger.LogDebug(
            "Wallet for user {UserExternalId} already exists; treating registration as an idempotent duplicate",
            wallet.UserExternalId);
        return CreateWalletResult.AlreadyExists;
    }
}

using Wallet.Application.Wallets;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Wallet.Tests.Application.Wallets;

public sealed class CreateWalletHandlerTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_CreatesZeroBalanceWallet()
    {
        var repository = new FakeWalletRepository(wasAdded: true);
        var handler = CreateHandler(repository);
        var userExternalId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new CreateWalletCommand(userExternalId, CreatedAt),
            CancellationToken.None);

        Assert.Equal(CreateWalletResult.Created, result);
        Assert.NotNull(repository.Wallet);
        Assert.Equal(userExternalId, repository.Wallet.UserExternalId);
        Assert.Equal(0, repository.Wallet.BalanceCents);
        Assert.Equal(CreatedAt, repository.Wallet.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAlreadyExistsForDuplicateEvent()
    {
        var repository = new FakeWalletRepository(wasAdded: false);
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(
            new CreateWalletCommand(Guid.NewGuid(), CreatedAt),
            CancellationToken.None);

        Assert.Equal(CreateWalletResult.AlreadyExists, result);
    }

    [Fact]
    public async Task HandleAsync_RejectsEmptyUserId()
    {
        var repository = new FakeWalletRepository(wasAdded: true);
        var handler = CreateHandler(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new CreateWalletCommand(Guid.Empty, CreatedAt),
            CancellationToken.None));

        Assert.Null(repository.Wallet);
    }

    private static CreateWalletHandler CreateHandler(IWalletRepository repository)
    {
        return new CreateWalletHandler(
            repository,
            NullLogger<CreateWalletHandler>.Instance);
    }

    private sealed class FakeWalletRepository(bool wasAdded) : IWalletRepository
    {
        public Domain.Wallets.Wallet? Wallet { get; private set; }

        public Task<bool> TryAddAsync(
            Domain.Wallets.Wallet wallet,
            CancellationToken cancellationToken)
        {
            Wallet = wallet;
            return Task.FromResult(wasAdded);
        }
    }
}

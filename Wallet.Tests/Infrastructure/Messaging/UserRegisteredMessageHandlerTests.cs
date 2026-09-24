using System.Text.Json;
using Common.Application;
using Common.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Wallet.Application.Wallets;
using Wallet.Infrastructure.Messaging;
using Xunit;

namespace Wallet.Tests.Infrastructure.Messaging;

public sealed class UserRegisteredMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeserializesEventAndCreatesWallet()
    {
        var commandHandler = new FakeCreateWalletHandler();
        var handler = CreateHandler(commandHandler);
        var message = new UserRegisteredMessage(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));

        await handler.HandleAsync(
            JsonSerializer.Serialize(message),
            CancellationToken.None);

        Assert.Equal(message.ExternalId, commandHandler.Command?.UserExternalId);
        Assert.Equal(message.CreatedAt, commandHandler.Command?.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_SkipsMalformedJson()
    {
        var commandHandler = new FakeCreateWalletHandler();
        var handler = CreateHandler(commandHandler);

        await handler.HandleAsync("{", CancellationToken.None);

        Assert.Null(commandHandler.Command);
    }

    [Fact]
    public async Task HandleAsync_SkipsRejectedMessage()
    {
        var commandHandler = new FakeCreateWalletHandler
        {
            Exception = new ArgumentException("Invalid user ID")
        };
        var handler = CreateHandler(commandHandler);

        await handler.HandleAsync(
            JsonSerializer.Serialize(new UserRegisteredMessage(
                Guid.Empty,
                DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.NotNull(commandHandler.Command);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSkipUnexpectedFailure()
    {
        var commandHandler = new FakeCreateWalletHandler
        {
            Exception = new InvalidOperationException("Database unavailable")
        };
        var handler = CreateHandler(commandHandler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            JsonSerializer.Serialize(new UserRegisteredMessage(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow)),
            CancellationToken.None));
    }

    private static UserRegisteredMessageHandler CreateHandler(
        FakeCreateWalletHandler commandHandler)
    {
        return new UserRegisteredMessageHandler(
            commandHandler,
            NullLogger<UserRegisteredMessageHandler>.Instance);
    }

    private sealed class FakeCreateWalletHandler
        : ICommandHandler<CreateWalletCommand, CreateWalletResult>
    {
        public CreateWalletCommand? Command { get; private set; }

        public Exception? Exception { get; init; }

        public Task<CreateWalletResult> HandleAsync(
            CreateWalletCommand command,
            CancellationToken cancellationToken)
        {
            Command = command;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(CreateWalletResult.Created);
        }
    }
}

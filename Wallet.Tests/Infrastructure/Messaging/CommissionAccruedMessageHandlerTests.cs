using System.Text.Json;
using Common.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Wallet.Application.Payouts;
using Wallet.Infrastructure.Messaging;
using Xunit;

namespace Wallet.Tests.Infrastructure.Messaging;

public sealed class CommissionAccruedMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeserializesEventAndProcessesPayout()
    {
        var processor = new FakePayoutProcessor();
        var handler = CreateHandler(processor);
        var message = CreateMessage();

        await handler.HandleAsync(
            JsonSerializer.Serialize(message),
            CancellationToken.None);

        Assert.Equal(message, processor.Message);
    }

    [Fact]
    public async Task HandleAsync_SkipsMalformedJson()
    {
        var processor = new FakePayoutProcessor();
        var handler = CreateHandler(processor);

        await handler.HandleAsync("{", CancellationToken.None);

        Assert.Null(processor.Message);
    }

    [Fact]
    public async Task HandleAsync_SkipsRejectedMessage()
    {
        var processor = new FakePayoutProcessor
        {
            Exception = new ArgumentException("Invalid commission")
        };
        var handler = CreateHandler(processor);

        await handler.HandleAsync(
            JsonSerializer.Serialize(CreateMessage()),
            CancellationToken.None);

        Assert.NotNull(processor.Message);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSkipUnexpectedFailure()
    {
        var processor = new FakePayoutProcessor
        {
            Exception = new InvalidOperationException("Database unavailable")
        };
        var handler = CreateHandler(processor);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            JsonSerializer.Serialize(CreateMessage()),
            CancellationToken.None));
    }

    private static CommissionAccruedMessageHandler CreateHandler(
        FakePayoutProcessor processor)
    {
        return new CommissionAccruedMessageHandler(
            processor,
            NullLogger<CommissionAccruedMessageHandler>.Instance);
    }

    private static CommissionAccruedMessage CreateMessage()
    {
        return new CommissionAccruedMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1_500,
            2,
            3,
            new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakePayoutProcessor : ICommissionPayoutProcessor
    {
        public CommissionAccruedMessage? Message { get; private set; }

        public Exception? Exception { get; init; }

        public Task<CommissionPayoutResult> ProcessAsync(
            CommissionAccruedMessage message,
            CancellationToken cancellationToken)
        {
            Message = message;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(CommissionPayoutResult.Paid);
        }
    }
}

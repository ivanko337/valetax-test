using System.Text.Json;
using Commissions.Application.Commissions;
using Commissions.Infrastructure.Messaging;
using Common.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commissions.Tests.Infrastructure.Messaging;

public sealed class CommissionPaidMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeserializesEventAndProcessesPayment()
    {
        var processor = new FakePaymentProcessor();
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
        var processor = new FakePaymentProcessor();
        var handler = CreateHandler(processor);

        await handler.HandleAsync("{", CancellationToken.None);

        Assert.Null(processor.Message);
    }

    [Fact]
    public async Task HandleAsync_SkipsRejectedMessage()
    {
        var processor = new FakePaymentProcessor
        {
            Exception = new ArgumentException("Invalid payment")
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
        var processor = new FakePaymentProcessor
        {
            Exception = new InvalidOperationException("Database unavailable")
        };
        var handler = CreateHandler(processor);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            JsonSerializer.Serialize(CreateMessage()),
            CancellationToken.None));
    }

    private static CommissionPaidMessageHandler CreateHandler(
        FakePaymentProcessor processor)
    {
        return new CommissionPaidMessageHandler(
            processor,
            NullLogger<CommissionPaidMessageHandler>.Instance);
    }

    private static CommissionPaidMessage CreateMessage()
    {
        return new CommissionPaidMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1_500,
            new DateTimeOffset(2026, 9, 24, 15, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakePaymentProcessor : ICommissionPaymentProcessor
    {
        public CommissionPaidMessage? Message { get; private set; }

        public Exception? Exception { get; init; }

        public Task<CommissionPaymentResult> ProcessAsync(
            CommissionPaidMessage message,
            CancellationToken cancellationToken)
        {
            Message = message;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(CommissionPaymentResult.Paid);
        }
    }
}

using System.Text.Json;
using Common.Constants;
using Common.Messages;
using Common.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Payouts;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Processing;
using Xunit;

namespace Wallet.Tests.Infrastructure.Processing;

public sealed class CommissionPayoutProcessorTests
{
    private static readonly DateTimeOffset PaidAt =
        new(2026, 9, 24, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_CreatesWalletPayoutAndPaidOutboxMessageAtomically()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = CreateMessage();
        var processor = fixture.CreateProcessor();

        var result = await processor.ProcessAsync(message, CancellationToken.None);

        Assert.Equal(CommissionPayoutResult.Paid, result);

        var wallet = await fixture.DbContext.Wallets.SingleAsync();
        Assert.Equal(message.UserExternalId, wallet.UserExternalId);
        Assert.Equal(message.AmountCents, wallet.BalanceCents);
        Assert.Equal(PaidAt, wallet.CreatedAt);

        var payout = await fixture.DbContext.Payouts.SingleAsync();
        Assert.Equal(message.CommissionId, payout.CommissionId);
        Assert.Equal(message.UserExternalId, payout.UserExternalId);
        Assert.Equal(message.AmountCents, payout.AmountCents);
        Assert.Equal(PaidAt, payout.PaidAt);

        var outboxMessage = await fixture.DbContext.OutboxMessages.SingleAsync();
        Assert.Equal(KafkaConstants.CommissionsPaidTopic, outboxMessage.Topic);
        Assert.Equal(message.UserExternalId.ToString("D"), outboxMessage.MessageKey);

        var paidMessage = JsonSerializer.Deserialize<CommissionPaidMessage>(
            outboxMessage.Payload);
        Assert.NotNull(paidMessage);
        Assert.Equal(message.CommissionId, paidMessage.CommissionId);
        Assert.Equal(message.UserExternalId, paidMessage.UserExternalId);
        Assert.Equal(message.AmountCents, paidMessage.AmountCents);
        Assert.Equal(PaidAt, paidMessage.PaidAt);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateCommissionDoesNotPayOrPublishTwice()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = CreateMessage();
        var processor = fixture.CreateProcessor();

        var firstResult = await processor.ProcessAsync(message, CancellationToken.None);
        var secondResult = await processor.ProcessAsync(message, CancellationToken.None);

        Assert.Equal(CommissionPayoutResult.Paid, firstResult);
        Assert.Equal(CommissionPayoutResult.AlreadyPaid, secondResult);
        Assert.Equal(
            message.AmountCents,
            (await fixture.DbContext.Wallets.SingleAsync()).BalanceCents);
        Assert.Single(await fixture.DbContext.Payouts.ToListAsync());
        Assert.Single(await fixture.DbContext.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task ProcessAsync_ConflictingDuplicateDoesNotCreateAnotherWallet()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = CreateMessage();
        var processor = fixture.CreateProcessor();
        await processor.ProcessAsync(message, CancellationToken.None);

        var conflictingDuplicate = message with
        {
            UserExternalId = Guid.NewGuid(),
            AmountCents = message.AmountCents + 1
        };

        var result = await processor.ProcessAsync(
            conflictingDuplicate,
            CancellationToken.None);

        Assert.Equal(CommissionPayoutResult.AlreadyPaid, result);
        var wallet = await fixture.DbContext.Wallets.SingleAsync();
        Assert.Equal(message.UserExternalId, wallet.UserExternalId);
        Assert.Equal(message.AmountCents, wallet.BalanceCents);
        Assert.Single(await fixture.DbContext.Payouts.ToListAsync());
        Assert.Single(await fixture.DbContext.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task ProcessAsync_CreditsExistingWallet()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = CreateMessage();
        var walletCreatedAt = PaidAt.AddDays(-1);
        fixture.DbContext.Wallets.Add(new Domain.Wallets.Wallet(
            message.UserExternalId,
            700,
            walletCreatedAt));
        await fixture.DbContext.SaveChangesAsync();

        await fixture.CreateProcessor().ProcessAsync(message, CancellationToken.None);

        var wallet = await fixture.DbContext.Wallets.SingleAsync();
        Assert.Equal(700 + message.AmountCents, wallet.BalanceCents);
        Assert.Equal(walletCreatedAt, wallet.CreatedAt);
    }

    [Fact]
    public async Task ProcessAsync_AppliesNegativeCommissionToBalance()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = CreateMessage() with { AmountCents = -500 };

        var result = await fixture.CreateProcessor().ProcessAsync(
            message,
            CancellationToken.None);

        Assert.Equal(CommissionPayoutResult.Paid, result);
        Assert.Equal(-500, (await fixture.DbContext.Wallets.SingleAsync()).BalanceCents);
        Assert.Equal(-500, (await fixture.DbContext.Payouts.SingleAsync()).AmountCents);
        Assert.Single(await fixture.DbContext.OutboxMessages.ToListAsync());
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
            PaidAt.AddMinutes(-1));
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(
            SqliteConnection connection,
            WalletDbContext dbContext)
        {
            Connection = connection;
            DbContext = dbContext;
        }

        private SqliteConnection Connection { get; }

        public WalletDbContext DbContext { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new WalletDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new TestFixture(connection, dbContext);
        }

        public CommissionPayoutProcessor CreateProcessor()
        {
            return new CommissionPayoutProcessor(
                DbContext,
                new EfOutboxWriter<WalletDbContext>(DbContext),
                new FixedTimeProvider(PaidAt),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CommissionPayoutProcessor>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}

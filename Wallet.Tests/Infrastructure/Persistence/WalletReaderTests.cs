using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Wallet.Tests.Infrastructure.Persistence;

public sealed class WalletReaderTests
{
    [Fact]
    public async Task GetBalanceAsync_ReturnsBalanceForExistingWallet()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var userExternalId = Guid.NewGuid();
        fixture.DbContext.Wallets.Add(new Domain.Wallets.Wallet(
            userExternalId,
            2_500,
            DateTimeOffset.UtcNow));
        await fixture.DbContext.SaveChangesAsync();

        var balance = await fixture.Reader.GetBalanceAsync(
            userExternalId,
            CancellationToken.None);

        Assert.NotNull(balance);
        Assert.Equal(userExternalId, balance.UserExternalId);
        Assert.Equal(2_500, balance.BalanceCents);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsNullForMissingWallet()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var balance = await fixture.Reader.GetBalanceAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(balance);
    }

    [Fact]
    public async Task GetPayoutHistoryAsync_ReturnsPayoutsNewestFirst()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var userExternalId = Guid.NewGuid();
        var otherUserExternalId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);
        fixture.DbContext.Wallets.AddRange(
            new Domain.Wallets.Wallet(userExternalId, 3_000, createdAt),
            new Domain.Wallets.Wallet(otherUserExternalId, 900, createdAt));

        var olderCommissionId = Guid.NewGuid();
        var newerCommissionId = Guid.NewGuid();
        fixture.DbContext.Payouts.AddRange(
            new Domain.Payouts.Payout(
                olderCommissionId,
                userExternalId,
                1_000,
                createdAt.AddMinutes(1)),
            new Domain.Payouts.Payout(
                newerCommissionId,
                userExternalId,
                2_000,
                createdAt.AddMinutes(2)),
            new Domain.Payouts.Payout(
                Guid.NewGuid(),
                otherUserExternalId,
                900,
                createdAt.AddMinutes(3)));
        await fixture.DbContext.SaveChangesAsync();

        var payouts = await fixture.Reader.GetPayoutHistoryAsync(
            userExternalId,
            CancellationToken.None);

        Assert.NotNull(payouts);
        Assert.Collection(
            payouts,
            payout =>
            {
                Assert.Equal(newerCommissionId, payout.CommissionId);
                Assert.Equal(2_000, payout.AmountCents);
                Assert.Equal(createdAt.AddMinutes(2), payout.PaidAt);
            },
            payout =>
            {
                Assert.Equal(olderCommissionId, payout.CommissionId);
                Assert.Equal(1_000, payout.AmountCents);
                Assert.Equal(createdAt.AddMinutes(1), payout.PaidAt);
            });
    }

    [Fact]
    public async Task GetPayoutHistoryAsync_ReturnsEmptyHistoryForWalletWithoutPayouts()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var userExternalId = Guid.NewGuid();
        fixture.DbContext.Wallets.Add(new Domain.Wallets.Wallet(
            userExternalId,
            0,
            DateTimeOffset.UtcNow));
        await fixture.DbContext.SaveChangesAsync();

        var payouts = await fixture.Reader.GetPayoutHistoryAsync(
            userExternalId,
            CancellationToken.None);

        Assert.NotNull(payouts);
        Assert.Empty(payouts);
    }

    [Fact]
    public async Task GetPayoutHistoryAsync_ReturnsNullForMissingWallet()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var payouts = await fixture.Reader.GetPayoutHistoryAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(payouts);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(
            SqliteConnection connection,
            WalletDbContext dbContext)
        {
            Connection = connection;
            DbContext = dbContext;
            Reader = new WalletReader(dbContext);
        }

        private SqliteConnection Connection { get; }

        public WalletDbContext DbContext { get; }

        public WalletReader Reader { get; }

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

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}

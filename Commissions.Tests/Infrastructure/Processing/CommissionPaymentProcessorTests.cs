using Commissions.Application.Commissions;
using Commissions.Domain.Commissions;
using Commissions.Domain.CommissionSchemes;
using Commissions.Domain.ProfitEvents;
using Commissions.Infrastructure.Persistence;
using Commissions.Infrastructure.Processing;
using Common.Enums;
using Common.Messages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Commissions.Tests.Infrastructure.Processing;

public sealed class CommissionPaymentProcessorTests
{
    private static readonly DateTimeOffset PaidAt =
        new(2026, 9, 24, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_MarksPendingCommissionAsPaid()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = fixture.CreateMessage();

        var result = await fixture.CreateProcessor().ProcessAsync(
            message,
            CancellationToken.None);

        Assert.Equal(CommissionPaymentResult.Paid, result);

        var commission = await fixture.DbContext.Commissions
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(CommissionPaymentStatus.Paid, commission.PaymentStatus);
        Assert.Equal(PaidAt, commission.PaidAt);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateDoesNotOverwritePaidAt()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var processor = fixture.CreateProcessor();
        var message = fixture.CreateMessage();
        await processor.ProcessAsync(message, CancellationToken.None);

        var result = await processor.ProcessAsync(
            message with { PaidAt = PaidAt.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(CommissionPaymentResult.AlreadyPaid, result);
        Assert.Equal(
            PaidAt,
            (await fixture.DbContext.Commissions.AsNoTracking().SingleAsync()).PaidAt);
    }

    [Fact]
    public async Task ProcessAsync_RejectsEventThatDoesNotMatchCommission()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = fixture.CreateMessage() with { AmountCents = 1_501 };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.CreateProcessor().ProcessAsync(message, CancellationToken.None));

        var commission = await fixture.DbContext.Commissions
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(CommissionPaymentStatus.Pending, commission.PaymentStatus);
        Assert.Null(commission.PaidAt);
    }

    [Fact]
    public async Task ProcessAsync_RejectsMissingCommission()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var message = fixture.CreateMessage() with { CommissionId = Guid.NewGuid() };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.CreateProcessor().ProcessAsync(message, CancellationToken.None));
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(
            SqliteConnection connection,
            CommissionsDbContext dbContext,
            Commission commission)
        {
            Connection = connection;
            DbContext = dbContext;
            Commission = commission;
        }

        private SqliteConnection Connection { get; }

        private Commission Commission { get; }

        public CommissionsDbContext DbContext { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<CommissionsDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new CommissionsDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var occurredAt = PaidAt.AddMinutes(-2);
            var profitEvent = new ProfitEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                100_000,
                occurredAt,
                occurredAt.AddSeconds(1));
            var scheme = new CommissionScheme(
                CommissionSchemaType.Linear,
                occurredAt);

            dbContext.AddRange(profitEvent, scheme);
            await dbContext.SaveChangesAsync();

            var commission = new Commission(
                Guid.NewGuid(),
                profitEvent.ExternalEventId,
                Guid.NewGuid(),
                1_500,
                scheme.Version,
                2);

            dbContext.Commissions.Add(commission);
            await dbContext.SaveChangesAsync();

            return new TestFixture(connection, dbContext, commission);
        }

        public CommissionPaymentProcessor CreateProcessor()
        {
            return new CommissionPaymentProcessor(DbContext);
        }

        public CommissionPaidMessage CreateMessage()
        {
            return new CommissionPaidMessage(
                Commission.Id,
                Commission.BeneficiaryId,
                Commission.AmountCents,
                PaidAt);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}

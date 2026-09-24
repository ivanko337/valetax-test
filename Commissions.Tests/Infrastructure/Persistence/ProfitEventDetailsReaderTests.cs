using Commissions.Domain.Commissions;
using Commissions.Domain.CommissionSchemes;
using Commissions.Domain.ProfitEvents;
using Commissions.Infrastructure.Persistence;
using Commissions.Infrastructure.Persistence.Repositories;
using Common.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Commissions.Tests.Infrastructure.Persistence;

public sealed class ProfitEventDetailsReaderTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_ReturnsEventAndAllCommissionsWithSchemeAndPaymentDetails()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var profitEvent = new ProfitEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100_000,
            OccurredAt,
            OccurredAt.AddSeconds(1));
        var scheme = new CommissionScheme(
            CommissionSchemaType.Fibonacci,
            OccurredAt.AddDays(-1));

        profitEvent.StartCalculation(Guid.NewGuid(), OccurredAt.AddSeconds(2));
        profitEvent.CompleteCalculation(
            scheme.SchemaType,
            OccurredAt.AddSeconds(3));

        dbContext.AddRange(profitEvent, scheme);
        await dbContext.SaveChangesAsync();

        var secondLevel = new Commission(
            Guid.NewGuid(),
            profitEvent.ExternalEventId,
            Guid.NewGuid(),
            2_000,
            scheme.Version,
            level: 2);
        var firstLevel = new Commission(
            Guid.NewGuid(),
            profitEvent.ExternalEventId,
            Guid.NewGuid(),
            1_000,
            scheme.Version,
            level: 1);
        var paidAt = OccurredAt.AddMinutes(5);
        secondLevel.MarkPaid(paidAt);

        dbContext.AddRange(secondLevel, firstLevel);
        await dbContext.SaveChangesAsync();

        var result = await new ProfitEventDetailsReader(dbContext).GetAsync(
            profitEvent.ExternalEventId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(profitEvent.ExternalEventId, result.ExternalEventId);
        Assert.Equal(profitEvent.UserExternalId, result.UserExternalId);
        Assert.Equal(profitEvent.ProfitCents, result.ProfitCents);
        Assert.Equal(ProfitEventStatus.Calculated, result.Status);
        Assert.Equal(CommissionSchemaType.Fibonacci, result.SchemaType);
        Assert.Equal(2, result.Commissions.Count);

        var pending = result.Commissions[0];
        Assert.Equal(firstLevel.Id, pending.CommissionId);
        Assert.Equal(firstLevel.BeneficiaryId, pending.PartnerExternalId);
        Assert.Equal(1, pending.Level);
        Assert.Equal(CommissionPaymentStatus.Pending, pending.PaymentStatus);
        Assert.Null(pending.PaidAt);
        Assert.Equal(scheme.Version, pending.SchemaVersion);
        Assert.Equal(CommissionSchemaType.Fibonacci, pending.SchemaType);

        var paid = result.Commissions[1];
        Assert.Equal(secondLevel.Id, paid.CommissionId);
        Assert.Equal(CommissionPaymentStatus.Paid, paid.PaymentStatus);
        Assert.Equal(paidAt, paid.PaidAt);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenEventDoesNotExist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var result = await new ProfitEventDetailsReader(dbContext).GetAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    private static CommissionsDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CommissionsDbContext>()
            .UseSqlite(connection)
            .Options;

        return new CommissionsDbContext(options);
    }
}

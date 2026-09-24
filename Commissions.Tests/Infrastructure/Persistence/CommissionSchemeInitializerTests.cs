using Commissions.Domain.CommissionSchemes;
using Commissions.Infrastructure.Persistence;
using Common.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Commissions.Tests.Infrastructure.Persistence;

public sealed class CommissionSchemeInitializerTests
{
    private static readonly DateTimeOffset StartedAt =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task InitializeAsync_CreatesLinearSchemeWhenNoneExists()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var result = await CommissionSchemeInitializer.InitializeAsync(
            dbContext,
            new FixedTimeProvider(StartedAt));

        var scheme = await dbContext.CommissionSchemes.SingleAsync();
        Assert.True(result.Created);
        Assert.Equal(scheme.Version, result.Version);
        Assert.Equal(CommissionSchemaType.Linear, result.SchemaType);
        Assert.Equal(CommissionSchemaType.Linear, scheme.SchemaType);
        Assert.Equal(StartedAt, scheme.ChangedAt);
    }

    [Fact]
    public async Task InitializeAsync_DoesNotAppendSchemeWhenOneExists()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.CommissionSchemes.Add(new CommissionScheme(
            CommissionSchemaType.Fibonacci,
            StartedAt.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var result = await CommissionSchemeInitializer.InitializeAsync(
            dbContext,
            new FixedTimeProvider(StartedAt));

        var scheme = await dbContext.CommissionSchemes.SingleAsync();
        Assert.False(result.Created);
        Assert.Equal(scheme.Version, result.Version);
        Assert.Equal(CommissionSchemaType.Fibonacci, result.SchemaType);
        Assert.Equal(CommissionSchemaType.Fibonacci, scheme.SchemaType);
        Assert.Equal(StartedAt.AddDays(-1), scheme.ChangedAt);
    }

    private static CommissionsDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CommissionsDbContext>()
            .UseSqlite(connection)
            .Options;

        return new CommissionsDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

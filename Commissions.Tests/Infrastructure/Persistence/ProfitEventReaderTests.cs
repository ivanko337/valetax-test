using Commissions.Domain.ProfitEvents;
using Commissions.Infrastructure.Persistence;
using Commissions.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Commissions.Tests.Infrastructure.Persistence;

public sealed class ProfitEventReaderTests
{
    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyUsersProfitEventsNewestFirst()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CommissionsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new CommissionsDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var userExternalId = Guid.NewGuid();
        var older = CreateEvent(userExternalId, Utc(10), 1_000);
        var newer = CreateEvent(userExternalId, Utc(12), 2_000);
        var otherUsersEvent = CreateEvent(Guid.NewGuid(), Utc(13), 3_000);
        dbContext.ProfitEvents.AddRange(older, newer, otherUsersEvent);
        await dbContext.SaveChangesAsync();

        var events = await new ProfitEventReader(dbContext).GetByUserAsync(
            userExternalId,
            CancellationToken.None);

        Assert.Collection(
            events,
            item =>
            {
                Assert.Equal(newer.ExternalEventId, item.ExternalEventId);
                Assert.Equal(2_000, item.ProfitCents);
            },
            item =>
            {
                Assert.Equal(older.ExternalEventId, item.ExternalEventId);
                Assert.Equal(1_000, item.ProfitCents);
            });
    }

    private static ProfitEvent CreateEvent(
        Guid userExternalId,
        DateTimeOffset occurredAt,
        long profitCents)
    {
        return new ProfitEvent(
            Guid.NewGuid(),
            userExternalId,
            profitCents,
            occurredAt,
            occurredAt.AddMinutes(1));
    }

    private static DateTimeOffset Utc(int hour)
    {
        return new DateTimeOffset(2026, 9, 24, hour, 0, 0, TimeSpan.Zero);
    }
}

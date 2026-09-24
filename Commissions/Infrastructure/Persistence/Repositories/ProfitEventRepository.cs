using Commissions.Application.ProfitEvents;
using Commissions.Domain.ProfitEvents;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Commissions.Infrastructure.Persistence.Repositories;

public sealed class ProfitEventRepository(CommissionsDbContext dbContext)
    : IProfitEventRepository
{
    private const string ProfitEventPrimaryKeyConstraint = "PK_ProfitEvents";

    public async Task<bool> TryAddAsync(
        ProfitEvent profitEvent,
        CancellationToken cancellationToken)
    {
        dbContext.ProfitEvents.Add(profitEvent);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (IsProfitEventPrimaryKeyViolation(exception))
        {
            dbContext.Entry(profitEvent).State = EntityState.Detached;
            return false;
        }
    }

    public Task<ProfitEvent?> FindByExternalEventIdAsync(
        Guid externalEventId,
        CancellationToken cancellationToken)
    {
        return dbContext.ProfitEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                profitEvent => profitEvent.ExternalEventId == externalEventId,
                cancellationToken);
    }

    private static bool IsProfitEventPrimaryKeyViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName == ProfitEventPrimaryKeyConstraint;
    }
}

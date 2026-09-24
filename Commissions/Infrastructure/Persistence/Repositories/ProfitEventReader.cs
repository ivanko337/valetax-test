using Commissions.Application.ProfitEvents;
using Microsoft.EntityFrameworkCore;

namespace Commissions.Infrastructure.Persistence.Repositories;

public sealed class ProfitEventReader(CommissionsDbContext dbContext)
    : IProfitEventReader
{
    public async Task<IReadOnlyList<ProfitEventListItem>> GetByUserAsync(
        Guid userExternalId,
        CancellationToken cancellationToken)
    {
        var profitEvents = await dbContext.ProfitEvents
            .AsNoTracking()
            .Where(profitEvent => profitEvent.UserExternalId == userExternalId)
            .Select(profitEvent => new ProfitEventListItem(
                profitEvent.ExternalEventId,
                profitEvent.ProfitCents,
                profitEvent.OcurredAt,
                profitEvent.ReceivedAt,
                profitEvent.Status,
                profitEvent.SchemaType,
                profitEvent.CalculatedAt))
            .ToArrayAsync(cancellationToken);

        return profitEvents
            .OrderByDescending(profitEvent => profitEvent.OccurredAt)
            .ThenBy(profitEvent => profitEvent.ExternalEventId)
            .ToArray();
    }
}

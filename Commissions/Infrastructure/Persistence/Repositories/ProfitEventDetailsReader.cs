using Commissions.Application.ProfitEvents;
using Microsoft.EntityFrameworkCore;

namespace Commissions.Infrastructure.Persistence.Repositories;

public sealed class ProfitEventDetailsReader(CommissionsDbContext dbContext)
    : IProfitEventDetailsReader
{
    public async Task<ProfitEventDetails?> GetAsync(
        Guid externalEventId,
        CancellationToken cancellationToken)
    {
        var profitEvent = await dbContext.ProfitEvents
            .AsNoTracking()
            .Where(item => item.ExternalEventId == externalEventId)
            .Select(item => new
            {
                item.ExternalEventId,
                item.UserExternalId,
                item.ProfitCents,
                item.OcurredAt,
                item.ReceivedAt,
                item.Status,
                item.SchemaType,
                item.CalculatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profitEvent is null)
        {
            return null;
        }

        var commissions = await (
                from commission in dbContext.Commissions.AsNoTracking()
                join scheme in dbContext.CommissionSchemes.AsNoTracking()
                    on commission.SchemaVersion equals scheme.Version
                where commission.ExternalEventId == externalEventId
                orderby commission.Level, commission.BeneficiaryId, commission.Id
                select new ProfitEventCommissionDetails(
                    commission.Id,
                    commission.BeneficiaryId,
                    commission.Level,
                    commission.AmountCents,
                    commission.SchemaVersion,
                    scheme.SchemaType,
                    commission.PaymentStatus,
                    commission.PaidAt))
            .ToArrayAsync(cancellationToken);

        return new ProfitEventDetails(
            profitEvent.ExternalEventId,
            profitEvent.UserExternalId,
            profitEvent.ProfitCents,
            profitEvent.OcurredAt,
            profitEvent.ReceivedAt,
            profitEvent.Status,
            profitEvent.SchemaType,
            profitEvent.CalculatedAt,
            commissions);
    }
}

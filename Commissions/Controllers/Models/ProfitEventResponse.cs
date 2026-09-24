using Commissions.Application.ProfitEvents;
using Commissions.Domain.ProfitEvents;
using Common.Enums;

namespace Commissions.Controllers.Models;

public sealed record ProfitEventResponse(
    Guid ExternalEventId,
    long ProfitCents,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    ProfitEventStatus Status,
    CommissionSchemaType? SchemaType,
    DateTimeOffset? CalculatedAt)
{
    public static ProfitEventResponse From(ProfitEventListItem profitEvent)
    {
        return new ProfitEventResponse(
            profitEvent.ExternalEventId,
            profitEvent.ProfitCents,
            profitEvent.OccurredAt,
            profitEvent.ReceivedAt,
            profitEvent.Status,
            profitEvent.SchemaType,
            profitEvent.CalculatedAt);
    }
}

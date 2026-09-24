using Commissions.Domain.ProfitEvents;
using Common.Enums;

namespace Commissions.Application.ProfitEvents;

public sealed record ProfitEventListItem(
    Guid ExternalEventId,
    long ProfitCents,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    ProfitEventStatus Status,
    CommissionSchemaType? SchemaType,
    DateTimeOffset? CalculatedAt);

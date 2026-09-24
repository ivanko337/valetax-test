using Commissions.Domain.Commissions;
using Commissions.Domain.ProfitEvents;
using Common.Enums;

namespace Commissions.Application.ProfitEvents;

public sealed record ProfitEventDetails(
    Guid ExternalEventId,
    Guid UserExternalId,
    long ProfitCents,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    ProfitEventStatus Status,
    CommissionSchemaType? SchemaType,
    DateTimeOffset? CalculatedAt,
    IReadOnlyList<ProfitEventCommissionDetails> Commissions);

public sealed record ProfitEventCommissionDetails(
    Guid CommissionId,
    Guid PartnerExternalId,
    int Level,
    long AmountCents,
    int SchemaVersion,
    CommissionSchemaType SchemaType,
    CommissionPaymentStatus PaymentStatus,
    DateTimeOffset? PaidAt);

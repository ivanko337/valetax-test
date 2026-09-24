using Commissions.Application.ProfitEvents;
using Commissions.Domain.Commissions;

namespace Commissions.Controllers.Models;

public sealed record ProfitEventDetailsResponse(
    Guid ExternalEventId,
    Guid UserExternalId,
    long ProfitCents,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    string Status,
    string? SchemaType,
    DateTimeOffset? CalculatedAt,
    IReadOnlyList<ProfitEventCommissionResponse> Commissions)
{
    internal static ProfitEventDetailsResponse From(ProfitEventDetails details)
    {
        return new ProfitEventDetailsResponse(
            details.ExternalEventId,
            details.UserExternalId,
            details.ProfitCents,
            details.OccurredAt,
            details.ReceivedAt,
            details.Status.ToString(),
            details.SchemaType?.ToString(),
            details.CalculatedAt,
            details.Commissions.Select(ProfitEventCommissionResponse.From).ToArray());
    }
}

public sealed record ProfitEventCommissionResponse(
    Guid CommissionId,
    Guid PartnerExternalId,
    int Level,
    long AmountCents,
    ProfitEventCommissionSchemeResponse Scheme,
    string PaymentStatus,
    bool IsPaid,
    DateTimeOffset? PaidAt)
{
    internal static ProfitEventCommissionResponse From(
        ProfitEventCommissionDetails details)
    {
        return new ProfitEventCommissionResponse(
            details.CommissionId,
            details.PartnerExternalId,
            details.Level,
            details.AmountCents,
            new ProfitEventCommissionSchemeResponse(
                details.SchemaVersion,
                details.SchemaType.ToString()),
            details.PaymentStatus.ToString(),
            details.PaymentStatus == CommissionPaymentStatus.Paid,
            details.PaidAt);
    }
}

public sealed record ProfitEventCommissionSchemeResponse(int Version, string Type);

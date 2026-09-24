namespace Commissions.Infrastructure.Processing.Models;

internal sealed record ProfitEventWorkItem(
    Guid ExternalEventId,
    Guid UserExternalId,
    long ProfitCents,
    int CalculationAttempts,
    Guid CalculationToken);

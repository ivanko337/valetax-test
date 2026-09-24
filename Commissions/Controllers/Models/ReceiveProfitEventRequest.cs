namespace Commissions.Controllers.Models;

public sealed record ReceiveProfitEventRequest(
    Guid ExternalEventId,
    Guid UserExternalId,
    long ProfitCents,
    DateTimeOffset OccurredAt);

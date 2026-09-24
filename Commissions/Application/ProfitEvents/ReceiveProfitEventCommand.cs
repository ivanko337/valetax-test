namespace Commissions.Application.ProfitEvents;

public sealed record ReceiveProfitEventCommand(
    Guid ExternalEventId,
    Guid UserExternalId,
    long ProfitCents,
    DateTimeOffset OcurredAt);

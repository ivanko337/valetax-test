namespace Commissions.Controllers.Models;

public sealed record ReceiveProfitEventResponse(
    Guid ExternalEventId,
    bool Duplicate);

using Commissions.Domain.ProfitEvents;

namespace Commissions.Application.ProfitEvents;

public interface IProfitEventRepository
{
    Task<bool> TryAddAsync(
        ProfitEvent profitEvent,
        CancellationToken cancellationToken);

    Task<ProfitEvent?> FindByExternalEventIdAsync(
        Guid externalEventId,
        CancellationToken cancellationToken);
}

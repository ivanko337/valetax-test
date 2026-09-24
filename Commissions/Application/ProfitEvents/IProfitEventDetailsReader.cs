namespace Commissions.Application.ProfitEvents;

public interface IProfitEventDetailsReader
{
    Task<ProfitEventDetails?> GetAsync(
        Guid externalEventId,
        CancellationToken cancellationToken);
}

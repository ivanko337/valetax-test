namespace Commissions.Application.ProfitEvents;

public interface IProfitEventReader
{
    Task<IReadOnlyList<ProfitEventListItem>> GetByUserAsync(
        Guid userExternalId,
        CancellationToken cancellationToken);
}

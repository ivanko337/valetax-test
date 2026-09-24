using Common.Application;

namespace Commissions.Application.ProfitEvents;

public sealed class GetUserProfitEventsHandler(IProfitEventReader profitEventReader)
    : IQueryHandler<GetUserProfitEventsQuery, IReadOnlyList<ProfitEventListItem>>
{
    public Task<IReadOnlyList<ProfitEventListItem>> HandleAsync(
        GetUserProfitEventsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserExternalId == Guid.Empty)
        {
            throw new ArgumentException(
                "User external ID must not be empty.",
                nameof(query));
        }

        return profitEventReader.GetByUserAsync(
            query.UserExternalId,
            cancellationToken);
    }
}

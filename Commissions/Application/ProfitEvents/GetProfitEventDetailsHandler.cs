using Common.Application;

namespace Commissions.Application.ProfitEvents;

public sealed class GetProfitEventDetailsHandler(
    IProfitEventDetailsReader profitEventDetailsReader)
    : IQueryHandler<GetProfitEventDetailsQuery, ProfitEventDetails?>
{
    public Task<ProfitEventDetails?> HandleAsync(
        GetProfitEventDetailsQuery query,
        CancellationToken cancellationToken)
    {
        return profitEventDetailsReader.GetAsync(
            query.ExternalEventId,
            cancellationToken);
    }
}

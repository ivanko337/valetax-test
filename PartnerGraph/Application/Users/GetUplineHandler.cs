using Common.Application;

namespace PartnerGraph.Application.Users;

public sealed class GetUplineHandler(IPartnerGraphReader partnerGraphReader)
    : IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?>
{
    public async Task<IReadOnlyList<PartnerAtLevel>?> HandleAsync(
        GetUplineQuery query,
        CancellationToken cancellationToken)
    {
        if (query.MaxPartners is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.MaxPartners),
                query.MaxPartners,
                "The maximum number of upline partners must be positive.");
        }

        var result = await partnerGraphReader.GetUplineAsync(
            query.ExternalId,
            cancellationToken);

        if (!result.UserExists)
        {
            return null;
        }

        return result.Partners
            .OrderBy(x => x.Level)
            .Take(query.MaxPartners ?? int.MaxValue)
            .ToArray();
    }
}

using Common.Application;
using PartnerGraph.Domain.Users;

namespace PartnerGraph.Application.Users;

public sealed class GetDownlineHandler(IPartnerGraphReader partnerGraphReader)
    : IQueryHandler<GetDownlineQuery, IReadOnlyList<User>?>
{
    public async Task<IReadOnlyList<User>?> HandleAsync(
        GetDownlineQuery query,
        CancellationToken cancellationToken)
    {
        var result = await partnerGraphReader.GetDownlineAsync(
            query.ExternalId,
            cancellationToken);

        return result.UserExists
            ? result.Partners
            : null;
    }
}

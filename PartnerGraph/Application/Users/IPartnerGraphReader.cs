using PartnerGraph.Domain.Users;

namespace PartnerGraph.Application.Users;

public interface IPartnerGraphReader
{
    Task<bool> PartnerChainContainsAsync(
        Guid partnerId,
        Guid externalId,
        CancellationToken cancellationToken);

    Task<PartnerTraversalResult<PartnerAtLevel>> GetUplineAsync(
        Guid externalId,
        CancellationToken cancellationToken);

    Task<PartnerTraversalResult<User>> GetDownlineAsync(
        Guid externalId,
        CancellationToken cancellationToken);
}

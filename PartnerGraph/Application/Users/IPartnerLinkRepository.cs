namespace PartnerGraph.Application.Users;

public interface IPartnerLinkRepository
{
    Task<UpdateUserPartnerResult> UpdateAsync(
        Guid externalId,
        Guid? partnerId,
        CancellationToken cancellationToken);
}

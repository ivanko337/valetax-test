using Commissions.Application.PartnerGraph.Models;

namespace Commissions.Application.PartnerGraph;

public interface IPartnerGraphClient
{
    Task<IReadOnlyList<UplinePartner>> GetUplinePartnersAsync(
        Guid userExternalId,
        CancellationToken cancellationToken);
}

using PartnerGraph.Application.Users;

namespace PartnerGraph.Controllers.Models;

public sealed record UplinePartnerResponse(
    Guid ExternalId,
    Guid? PartnerId,
    DateTimeOffset CreatedAt,
    int Level)
{
    internal static UplinePartnerResponse From(PartnerAtLevel partner)
    {
        return new UplinePartnerResponse(
            partner.User.ExternalId,
            partner.User.PartnerId,
            partner.User.CreatedAt,
            partner.Level);
    }
}

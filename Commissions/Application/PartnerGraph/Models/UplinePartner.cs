namespace Commissions.Application.PartnerGraph.Models;

public sealed record UplinePartner(Guid ExternalUserId, int Level);

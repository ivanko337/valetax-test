namespace PartnerGraph.Application.Users;

public sealed record GetUplineQuery(Guid ExternalId, int? MaxPartners = null);

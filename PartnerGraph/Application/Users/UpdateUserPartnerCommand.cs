namespace PartnerGraph.Application.Users;

public sealed record UpdateUserPartnerCommand(Guid ExternalId, Guid? PartnerId);

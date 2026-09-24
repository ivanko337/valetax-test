namespace PartnerGraph.Application.Users;

public sealed record CreateUserCommand(Guid ExternalId, Guid? PartnerId);

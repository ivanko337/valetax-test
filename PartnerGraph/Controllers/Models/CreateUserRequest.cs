namespace PartnerGraph.Controllers.Models;

public sealed record CreateUserRequest(Guid ExternalId, Guid? PartnerId);

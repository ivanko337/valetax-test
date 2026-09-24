using PartnerGraph.Domain.Users;

namespace PartnerGraph.Application.Users;

public sealed record CreateUserResult(CreateUserStatus Status, User? User = null);

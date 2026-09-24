using PartnerGraph.Domain.Users;

namespace PartnerGraph.Application.Users;

public sealed record UpdateUserPartnerResult(
    UpdateUserPartnerStatus Status,
    User? User = null);

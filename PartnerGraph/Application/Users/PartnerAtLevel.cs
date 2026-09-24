using PartnerGraph.Domain.Users;

namespace PartnerGraph.Application.Users;

public sealed record PartnerAtLevel(User User, int Level);

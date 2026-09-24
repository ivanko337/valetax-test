namespace PartnerGraph.Application.Users;

public sealed record PartnerTraversalResult<T>(
    bool UserExists,
    IReadOnlyList<T> Partners);

namespace PartnerGraph.Application.Users;

public sealed class PartnerGraphCycleException(Guid externalId, string direction)
    : InvalidOperationException(
        $"A cycle was detected while traversing the {direction} partners of user '{externalId}'.")
{
}

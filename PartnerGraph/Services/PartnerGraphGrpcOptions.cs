namespace PartnerGraph.Services;

public sealed class PartnerGraphGrpcOptions
{
    public const string SectionName = "PartnerGraphGrpc";

    public int MaxUplinePartners { get; init; } = 10;
}

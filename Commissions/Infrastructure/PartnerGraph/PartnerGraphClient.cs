using Commissions.Application.PartnerGraph;
using PartnerGraph.Grpc;
using ApplicationUplinePartner = Commissions.Application.PartnerGraph.Models.UplinePartner;

namespace Commissions.Infrastructure.PartnerGraph;

public sealed class PartnerGraphClient(
    PartnerGraphApi.PartnerGraphApiClient client)
    : IPartnerGraphClient
{
    public async Task<IReadOnlyList<ApplicationUplinePartner>> GetUplinePartnersAsync(
        Guid userExternalId,
        CancellationToken cancellationToken)
    {
        var response = await client.GetUplinePartnersAsync(
            new GetUplinePartnersRequest
            {
                ExternalUserId = userExternalId.ToString("D")
            },
            cancellationToken: cancellationToken);

        return response.Partners
            .Select(partner => new ApplicationUplinePartner(
                ParseExternalUserId(partner.ExternalUserId),
                partner.Level))
            .ToArray();
    }

    private static Guid ParseExternalUserId(string value)
    {
        if (!Guid.TryParse(value, out var externalUserId)
            || externalUserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Partner Graph returned invalid external user ID '{value}'.");
        }

        return externalUserId;
    }
}

using Common.Application;
using Grpc.Core;
using Microsoft.Extensions.Options;
using PartnerGraph.Application.Users;
using PartnerGraph.Grpc;

namespace PartnerGraph.Services;

public sealed class PartnerGraphGrpcService(
    IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?> getUplineHandler,
    IOptions<PartnerGraphGrpcOptions> options,
    ILogger<PartnerGraphGrpcService> logger)
    : PartnerGraphApi.PartnerGraphApiBase
{
    private readonly int _maxUplinePartners = options.Value.MaxUplinePartners;

    public override async Task<GetUplinePartnersResponse> GetUplinePartners(
        GetUplinePartnersRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ExternalUserId, out var externalId)
            || externalId == Guid.Empty)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "external_user_id must be a non-empty UUID."));
        }

        try
        {
            var partners = await getUplineHandler.HandleAsync(
                new GetUplineQuery(externalId, _maxUplinePartners),
                context.CancellationToken);

            if (partners is null)
            {
                throw new RpcException(new Status(
                    StatusCode.NotFound,
                    $"User '{externalId}' does not exist."));
            }

            var response = new GetUplinePartnersResponse();
            response.Partners.AddRange(partners.Select(partner => new UplinePartner
            {
                ExternalUserId = partner.User.ExternalId.ToString("D"),
                Level = partner.Level
            }));

            return response;
        }
        catch (PartnerGraphCycleException exception)
        {
            logger.LogError(
                exception,
                "A cycle was detected while retrieving upline partners for {ExternalUserId}.",
                externalId);

            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    "The partner graph is corrupted: an upline cycle was detected."),
                new Metadata { { "error-code", "partner_cycle_detected" } });
        }
    }
}

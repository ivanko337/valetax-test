using Common.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PartnerGraph.Application.Users;
using PartnerGraph.Domain.Users;
using PartnerGraph.Grpc;
using PartnerGraph.Services;
using Xunit;

namespace PartnerGraph.Tests.Services;

public sealed class PartnerGraphGrpcServiceTests
{
    private static readonly ServerCallContext CallContext = new TestServerCallContext();

    [Fact]
    public async Task GetUplinePartners_ReturnsIdsAndLevelsUsingConfiguredMaximum()
    {
        var partners = new[]
        {
            CreatePartner(1),
            CreatePartner(2)
        };
        var handler = new StubUplineHandler(partners);
        var service = CreateService(handler, maxUplinePartners: 2);
        var externalId = Guid.NewGuid();

        var reply = await service.GetUplinePartners(
            new GetUplinePartnersRequest { ExternalUserId = externalId.ToString() },
            CallContext);

        Assert.Equal(2, handler.ReceivedQuery?.MaxPartners);
        Assert.Equal(externalId, handler.ReceivedQuery?.ExternalId);
        Assert.Equal(
            partners.Select(x => x.User.ExternalId.ToString("D")),
            reply.Partners.Select(x => x.ExternalUserId));
        Assert.Equal([1, 2], reply.Partners.Select(x => x.Level));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task GetUplinePartners_RejectsInvalidExternalUserId(string externalUserId)
    {
        var handler = new StubUplineHandler([]);
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetUplinePartners(
                new GetUplinePartnersRequest { ExternalUserId = externalUserId },
                CallContext));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Null(handler.ReceivedQuery);
    }

    [Fact]
    public async Task GetUplinePartners_ReturnsNotFoundWhenUserDoesNotExist()
    {
        var service = CreateService(new StubUplineHandler(null));

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetUplinePartners(
                new GetUplinePartnersRequest { ExternalUserId = Guid.NewGuid().ToString() },
                CallContext));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetUplinePartners_ReturnsInternalWhenGraphContainsCycle()
    {
        var service = CreateService(new CycleUplineHandler());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetUplinePartners(
                new GetUplinePartnersRequest { ExternalUserId = Guid.NewGuid().ToString() },
                CallContext));

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Contains(
            exception.Trailers,
            entry => entry.Key == "error-code" && entry.Value == "partner_cycle_detected");
    }

    private static PartnerGraphGrpcService CreateService(
        IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?> handler,
        int maxUplinePartners = 10)
    {
        return new PartnerGraphGrpcService(
            handler,
            Options.Create(new PartnerGraphGrpcOptions
            {
                MaxUplinePartners = maxUplinePartners
            }),
            NullLogger<PartnerGraphGrpcService>.Instance);
    }

    private static PartnerAtLevel CreatePartner(int level)
    {
        return new PartnerAtLevel(
            new User(Guid.NewGuid(), null, DateTimeOffset.UtcNow),
            level);
    }

    private sealed class StubUplineHandler(IReadOnlyList<PartnerAtLevel>? result)
        : IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?>
    {
        public GetUplineQuery? ReceivedQuery { get; private set; }

        public Task<IReadOnlyList<PartnerAtLevel>?> HandleAsync(
            GetUplineQuery query,
            CancellationToken cancellationToken)
        {
            ReceivedQuery = query;
            return Task.FromResult(result);
        }
    }

    private sealed class CycleUplineHandler
        : IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?>
    {
        public Task<IReadOnlyList<PartnerAtLevel>?> HandleAsync(
            GetUplineQuery query,
            CancellationToken cancellationToken)
        {
            throw new PartnerGraphCycleException(query.ExternalId, "upline");
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata _responseTrailers = [];
        private readonly Dictionary<object, object> _userState = [];

        protected override string MethodCore => "partnergraph.v1.PartnerGraphApi/GetUplinePartners";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:0";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => _responseTrailers;
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => null!;
        protected override IDictionary<object, object> UserStateCore => _userState;

        protected override ContextPropagationToken CreatePropagationTokenCore(
            ContextPropagationOptions? options)
        {
            throw new NotSupportedException();
        }

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            return Task.CompletedTask;
        }
    }
}

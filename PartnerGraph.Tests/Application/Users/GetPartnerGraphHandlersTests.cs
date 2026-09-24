using PartnerGraph.Application.Users;
using PartnerGraph.Domain.Users;
using Xunit;

namespace PartnerGraph.Tests.Application.Users;

public sealed class GetPartnerGraphHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetUpline_SortsPartnersByLevel()
    {
        var repository = new FakePartnerGraphReader
        {
            Upline =
            [
                new PartnerAtLevel(CreateUser(), 3),
                new PartnerAtLevel(CreateUser(), 1),
                new PartnerAtLevel(CreateUser(), 2)
            ]
        };
        var externalId = Guid.NewGuid();
        var handler = new GetUplineHandler(repository);

        var result = await handler.HandleAsync(
            new GetUplineQuery(externalId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result.Select(x => x.Level));
    }

    [Fact]
    public async Task GetUpline_ReturnsEmptyArrayWhenUserHasNoPartner()
    {
        var repository = new FakePartnerGraphReader();
        var externalId = Guid.NewGuid();
        var handler = new GetUplineHandler(repository);

        var result = await handler.HandleAsync(
            new GetUplineQuery(externalId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUpline_ReturnsConfiguredMaximumAfterSortingByLevel()
    {
        var repository = new FakePartnerGraphReader
        {
            Upline =
            [
                new PartnerAtLevel(CreateUser(), 3),
                new PartnerAtLevel(CreateUser(), 1),
                new PartnerAtLevel(CreateUser(), 2)
            ]
        };
        var externalId = Guid.NewGuid();
        var handler = new GetUplineHandler(repository);

        var result = await handler.HandleAsync(
            new GetUplineQuery(externalId, MaxPartners: 2),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal([1, 2], result.Select(x => x.Level));
    }

    [Fact]
    public async Task GetUpline_RejectsNonPositiveMaximumWithoutQueryingRepository()
    {
        var repository = new FakePartnerGraphReader();
        var handler = new GetUplineHandler(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.HandleAsync(
            new GetUplineQuery(Guid.NewGuid(), MaxPartners: 0),
            CancellationToken.None));

        Assert.Equal(0, repository.UplineQueries);
    }

    [Fact]
    public async Task GetDownline_ReturnsAllRepositoryResults()
    {
        var expected = new[] { CreateUser(), CreateUser(), CreateUser() };
        var repository = new FakePartnerGraphReader { Downline = expected };
        var externalId = Guid.NewGuid();
        var handler = new GetDownlineHandler(repository);

        var result = await handler.HandleAsync(
            new GetDownlineQuery(externalId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Queries_ReturnNullWhenUserDoesNotExist()
    {
        var repository = new FakePartnerGraphReader
        {
            UserExists = false
        };
        var externalId = Guid.NewGuid();

        var upline = await new GetUplineHandler(repository).HandleAsync(
            new GetUplineQuery(externalId),
            CancellationToken.None);
        var downline = await new GetDownlineHandler(repository).HandleAsync(
            new GetDownlineQuery(externalId),
            CancellationToken.None);

        Assert.Null(upline);
        Assert.Null(downline);
        Assert.Equal(1, repository.UplineQueries);
        Assert.Equal(1, repository.DownlineQueries);
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), null, Now);
    }

    private sealed class FakePartnerGraphReader : IPartnerGraphReader
    {
        public bool UserExists { get; init; } = true;

        public IReadOnlyList<PartnerAtLevel> Upline { get; init; } = [];

        public IReadOnlyList<User> Downline { get; init; } = [];

        public int UplineQueries { get; private set; }

        public int DownlineQueries { get; private set; }

        public Task<bool> PartnerChainContainsAsync(
            Guid partnerId,
            Guid externalId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<PartnerTraversalResult<PartnerAtLevel>> GetUplineAsync(
            Guid externalId,
            CancellationToken cancellationToken)
        {
            UplineQueries++;
            return Task.FromResult(new PartnerTraversalResult<PartnerAtLevel>(
                UserExists,
                Upline));
        }

        public Task<PartnerTraversalResult<User>> GetDownlineAsync(
            Guid externalId,
            CancellationToken cancellationToken)
        {
            DownlineQueries++;
            return Task.FromResult(new PartnerTraversalResult<User>(
                UserExists,
                Downline));
        }

    }
}

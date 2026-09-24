using Commissions.Application.ProfitEvents;
using Xunit;

namespace Commissions.Tests.Application.ProfitEvents;

public sealed class GetUserProfitEventsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReadsEventsForRequestedUser()
    {
        var userExternalId = Guid.NewGuid();
        var reader = new StubReader([]);
        var handler = new GetUserProfitEventsHandler(reader);

        var result = await handler.HandleAsync(
            new GetUserProfitEventsQuery(userExternalId),
            CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(userExternalId, reader.UserExternalId);
    }

    [Fact]
    public async Task HandleAsync_RejectsEmptyUserId()
    {
        var handler = new GetUserProfitEventsHandler(new StubReader([]));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new GetUserProfitEventsQuery(Guid.Empty),
            CancellationToken.None));
    }

    private sealed class StubReader(IReadOnlyList<ProfitEventListItem> result)
        : IProfitEventReader
    {
        public Guid? UserExternalId { get; private set; }

        public Task<IReadOnlyList<ProfitEventListItem>> GetByUserAsync(
            Guid userExternalId,
            CancellationToken cancellationToken)
        {
            UserExternalId = userExternalId;
            return Task.FromResult(result);
        }
    }
}

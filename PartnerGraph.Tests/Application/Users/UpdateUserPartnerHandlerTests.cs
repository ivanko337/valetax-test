using PartnerGraph.Application.Users;
using PartnerGraph.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace PartnerGraph.Tests.Application.Users;

public sealed class UpdateUserPartnerHandlerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_RejectsEmptyIds(bool externalIdIsEmpty)
    {
        var repository = new StubPartnerLinkRepository();
        var handler = CreateHandler(repository);
        var command = externalIdIsEmpty
            ? new UpdateUserPartnerCommand(Guid.Empty, null)
            : new UpdateUserPartnerCommand(Guid.NewGuid(), Guid.Empty);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(
            externalIdIsEmpty
                ? UpdateUserPartnerStatus.InvalidExternalId
                : UpdateUserPartnerStatus.InvalidPartnerId,
            result.Status);
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public async Task HandleAsync_RejectsSelfReferenceWithoutCallingRepository()
    {
        var repository = new StubPartnerLinkRepository();
        var handler = CreateHandler(repository);
        var externalId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new UpdateUserPartnerCommand(externalId, externalId),
            CancellationToken.None);

        Assert.Equal(UpdateUserPartnerStatus.CycleDetected, result.Status);
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public async Task HandleAsync_DelegatesNullablePartnerLinkToAtomicRepository()
    {
        var user = new User(Guid.NewGuid(), null, DateTimeOffset.UtcNow);
        var repository = new StubPartnerLinkRepository
        {
            Result = new UpdateUserPartnerResult(UpdateUserPartnerStatus.Updated, user)
        };
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(
            new UpdateUserPartnerCommand(user.ExternalId, null),
            CancellationToken.None);

        Assert.Same(repository.Result, result);
        Assert.Equal(user.ExternalId, repository.ExternalId);
        Assert.Null(repository.PartnerId);
        Assert.Equal(1, repository.UpdateCalls);
    }

    private static UpdateUserPartnerHandler CreateHandler(
        IPartnerLinkRepository repository)
    {
        return new UpdateUserPartnerHandler(
            repository,
            NullLogger<UpdateUserPartnerHandler>.Instance);
    }

    private sealed class StubPartnerLinkRepository : IPartnerLinkRepository
    {
        public UpdateUserPartnerResult Result { get; init; } =
            new(UpdateUserPartnerStatus.UserDoesNotExist);

        public int UpdateCalls { get; private set; }

        public Guid? ExternalId { get; private set; }

        public Guid? PartnerId { get; private set; }

        public Task<UpdateUserPartnerResult> UpdateAsync(
            Guid externalId,
            Guid? partnerId,
            CancellationToken cancellationToken)
        {
            UpdateCalls++;
            ExternalId = externalId;
            PartnerId = partnerId;
            return Task.FromResult(Result);
        }
    }
}

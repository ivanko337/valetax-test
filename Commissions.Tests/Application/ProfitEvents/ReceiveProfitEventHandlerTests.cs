using Commissions.Application.ProfitEvents;
using Commissions.Domain.ProfitEvents;
using Xunit;

namespace Commissions.Tests.Application.ProfitEvents;

public sealed class ReceiveProfitEventHandlerTests
{
    private static readonly DateTimeOffset ReceivedAt =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_AddsPendingProfitEvent()
    {
        var repository = new StubRepository(wasAdded: true);
        var handler = CreateHandler(repository);
        var command = CreateCommand();

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(ReceiveProfitEventResult.Accepted, result);
        Assert.NotNull(repository.Added);
        Assert.Equal(command.ExternalEventId, repository.Added.ExternalEventId);
        Assert.Equal(command.UserExternalId, repository.Added.UserExternalId);
        Assert.Equal(command.ProfitCents, repository.Added.ProfitCents);
        Assert.Equal(command.OcurredAt, repository.Added.OcurredAt);
        Assert.Equal(ReceivedAt, repository.Added.ReceivedAt);
        Assert.Equal(ProfitEventStatus.PendingCalculation, repository.Added.Status);
        Assert.Null(repository.Added.SchemaType);
        Assert.Null(repository.Added.CalculatedAt);
    }

    [Fact]
    public async Task HandleAsync_TreatsSameExistingEventAsIdempotent()
    {
        var command = CreateCommand();
        var existing = CreateProfitEvent(command);
        var handler = CreateHandler(new StubRepository(false, existing));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(ReceiveProfitEventResult.AlreadyAccepted, result);
    }

    [Fact]
    public async Task HandleAsync_ReportsConflictingDuplicate()
    {
        var command = CreateCommand();
        var existing = new ProfitEvent(
            command.ExternalEventId,
            command.UserExternalId,
            command.ProfitCents + 1,
            command.OcurredAt,
            ReceivedAt);
        var handler = CreateHandler(new StubRepository(false, existing));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(ReceiveProfitEventResult.ConflictingDuplicate, result);
    }

    [Fact]
    public async Task HandleAsync_NormalizesOccurredAtToUtcForPostgreSql()
    {
        var repository = new StubRepository(wasAdded: true);
        var handler = CreateHandler(repository);
        var command = CreateCommand() with
        {
            OcurredAt = new DateTimeOffset(
                2026,
                9,
                24,
                15,
                0,
                0,
                TimeSpan.FromHours(3))
        };

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(TimeSpan.Zero, repository.Added?.OcurredAt.Offset);
        Assert.Equal(
            new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero),
            repository.Added?.OcurredAt);
    }

    private static ReceiveProfitEventHandler CreateHandler(
        IProfitEventRepository repository)
    {
        return new ReceiveProfitEventHandler(
            repository,
            new FixedTimeProvider(ReceivedAt));
    }

    private static ReceiveProfitEventCommand CreateCommand()
    {
        return new ReceiveProfitEventCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            25_000,
            new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));
    }

    private static ProfitEvent CreateProfitEvent(ReceiveProfitEventCommand command)
    {
        return new ProfitEvent(
            command.ExternalEventId,
            command.UserExternalId,
            command.ProfitCents,
            command.OcurredAt,
            ReceivedAt);
    }

    private sealed class StubRepository(
        bool wasAdded,
        ProfitEvent? existing = null)
        : IProfitEventRepository
    {
        public ProfitEvent? Added { get; private set; }

        public Task<bool> TryAddAsync(
            ProfitEvent profitEvent,
            CancellationToken cancellationToken)
        {
            Added = profitEvent;
            return Task.FromResult(wasAdded);
        }

        public Task<ProfitEvent?> FindByExternalEventIdAsync(
            Guid externalEventId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(existing);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}

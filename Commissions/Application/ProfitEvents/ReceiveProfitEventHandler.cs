using Common.Application;

namespace Commissions.Application.ProfitEvents;

public sealed class ReceiveProfitEventHandler(
    IProfitEventRepository profitEventRepository,
    TimeProvider timeProvider)
    : ICommandHandler<ReceiveProfitEventCommand, ReceiveProfitEventResult>
{
    public async Task<ReceiveProfitEventResult> HandleAsync(
        ReceiveProfitEventCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        var profitEvent = new Domain.ProfitEvents.ProfitEvent(
            command.ExternalEventId,
            command.UserExternalId,
            command.ProfitCents,
            command.OcurredAt.ToUniversalTime(),
            timeProvider.GetUtcNow());

        if (await profitEventRepository.TryAddAsync(profitEvent, cancellationToken))
        {
            return ReceiveProfitEventResult.Accepted;
        }

        var existing = await profitEventRepository.FindByExternalEventIdAsync(
            command.ExternalEventId,
            cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException(
                $"Profit event '{command.ExternalEventId}' conflicted but could not be loaded.");
        }

        return existing.HasSamePayload(
            command.UserExternalId,
            command.ProfitCents,
            command.OcurredAt)
            ? ReceiveProfitEventResult.AlreadyAccepted
            : ReceiveProfitEventResult.ConflictingDuplicate;
    }

    private static void Validate(ReceiveProfitEventCommand command)
    {
        if (command.ExternalEventId == Guid.Empty)
        {
            throw new ArgumentException(
                "External event ID must not be empty.",
                nameof(command));
        }

        if (command.UserExternalId == Guid.Empty)
        {
            throw new ArgumentException(
                "User external ID must not be empty.",
                nameof(command));
        }
    }
}

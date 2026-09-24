using Common.Application;

namespace PartnerGraph.Application.Users;

public sealed class UpdateUserPartnerHandler(
    IPartnerLinkRepository partnerLinkRepository,
    ILogger<UpdateUserPartnerHandler> logger)
    : ICommandHandler<UpdateUserPartnerCommand, UpdateUserPartnerResult>
{
    public Task<UpdateUserPartnerResult> HandleAsync(
        UpdateUserPartnerCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ExternalId == Guid.Empty)
        {
            return Task.FromResult(new UpdateUserPartnerResult(
                UpdateUserPartnerStatus.InvalidExternalId));
        }

        if (command.PartnerId == Guid.Empty)
        {
            return Task.FromResult(new UpdateUserPartnerResult(
                UpdateUserPartnerStatus.InvalidPartnerId));
        }

        if (command.PartnerId == command.ExternalId)
        {
            logger.LogWarning(
                "Rejected self-referencing partner link for user {ExternalId}",
                command.ExternalId);
            return Task.FromResult(new UpdateUserPartnerResult(
                UpdateUserPartnerStatus.CycleDetected));
        }

        return UpdateAsync(command, cancellationToken);
    }

    private async Task<UpdateUserPartnerResult> UpdateAsync(
        UpdateUserPartnerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await partnerLinkRepository.UpdateAsync(
            command.ExternalId,
            command.PartnerId,
            cancellationToken);

        switch (result.Status)
        {
            case UpdateUserPartnerStatus.Updated:
                logger.LogInformation(
                    "Updated partner link for user {ExternalId} to {PartnerId}",
                    command.ExternalId,
                    command.PartnerId);
                break;
            case UpdateUserPartnerStatus.Unchanged:
                logger.LogDebug(
                    "Partner link for user {ExternalId} is already {PartnerId}; no change was required",
                    command.ExternalId,
                    command.PartnerId);
                break;
            case UpdateUserPartnerStatus.CycleDetected:
                logger.LogWarning(
                    "Rejected partner link from user {ExternalId} to {PartnerId} because it would create a cycle",
                    command.ExternalId,
                    command.PartnerId);
                break;
        }

        return result;
    }
}

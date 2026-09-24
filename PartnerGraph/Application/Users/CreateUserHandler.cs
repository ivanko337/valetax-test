using Common.Application;
using Common.Constants;
using Common.Messages;
using Common.Outbox;
using PartnerGraph.Domain.Users;

namespace PartnerGraph.Application.Users;

public sealed class CreateUserHandler(
    IUserRepository userRepository,
    IOutboxWriter outbox,
    TimeProvider timeProvider,
    ILogger<CreateUserHandler> logger)
    : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    public async Task<CreateUserResult> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ExternalId == Guid.Empty)
        {
            return new CreateUserResult(CreateUserStatus.InvalidExternalId);
        }

        if (command.PartnerId == Guid.Empty)
        {
            return new CreateUserResult(CreateUserStatus.InvalidPartnerId);
        }

        var existingUser = await userRepository.FindByExternalIdAsync(
            command.ExternalId,
            cancellationToken);
        if (existingUser is not null)
        {
            return ResolveExistingUser(existingUser, command.PartnerId);
        }

        if (command.PartnerId is { } partnerId)
        {
            if (partnerId == command.ExternalId)
            {
                logger.LogWarning(
                    "Rejected self-referencing partner link for user {ExternalId}",
                    command.ExternalId);
                return new CreateUserResult(CreateUserStatus.CycleDetected);
            }

            if (!await userRepository.ExistsAsync(partnerId, cancellationToken))
            {
                return new CreateUserResult(CreateUserStatus.PartnerDoesNotExist);
            }
        }

        var user = new User(
            command.ExternalId,
            command.PartnerId,
            timeProvider.GetUtcNow());

        outbox.Add(
            KafkaConstants.UserRegisteredTopic,
            new UserRegisteredMessage(
                user.ExternalId,
                user.CreatedAt),
            user.ExternalId.ToString());

        var insertOutcome = await userRepository.TryAddAsync(user, cancellationToken);

        switch (insertOutcome)
        {
            case AddUserResult.Added:
                logger.LogInformation(
                    "Created user {ExternalId} with partner {PartnerId}",
                    user.ExternalId,
                    user.PartnerId);
                return new CreateUserResult(CreateUserStatus.Created, user);
            case AddUserResult.PartnerDoesNotExist:
                return new CreateUserResult(CreateUserStatus.PartnerDoesNotExist);
        }

        // Another request inserted this external ID after our initial lookup.
        // Re-read the winner so an identical request can complete idempotently.
        existingUser = await userRepository.FindByExternalIdAsync(
            command.ExternalId,
            cancellationToken);
        if (existingUser is null)
        {
            throw new InvalidOperationException(
                $"User '{command.ExternalId}' caused a unique-key conflict but could not be reloaded.");
        }

        return ResolveExistingUser(existingUser, command.PartnerId);
    }

    private CreateUserResult ResolveExistingUser(User user, Guid? requestedPartnerId)
    {
        if (user.PartnerId == requestedPartnerId)
        {
            logger.LogDebug(
                "User {ExternalId} already exists with partner {PartnerId}; treating registration as an idempotent duplicate",
                user.ExternalId,
                user.PartnerId);
            return new CreateUserResult(CreateUserStatus.AlreadyExists, user);
        }

        logger.LogWarning(
            "Rejected conflicting registration for user {ExternalId}: existing partner {ExistingPartnerId}, requested partner {RequestedPartnerId}",
            user.ExternalId,
            user.PartnerId,
            requestedPartnerId);
        return new CreateUserResult(CreateUserStatus.ExternalIdConflict, user);
    }
}

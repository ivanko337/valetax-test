using System.Text.Json;
using Common.Application;
using Common.Kafka;
using Common.Messages;
using Wallet.Application.Wallets;

namespace Wallet.Infrastructure.Messaging;

public sealed class UserRegisteredMessageHandler(
    ICommandHandler<CreateWalletCommand, CreateWalletResult> createWalletHandler,
    ILogger<UserRegisteredMessageHandler> logger)
    : IKafkaMessageHandler
{
    public async Task HandleAsync(
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var userRegistered = JsonSerializer.Deserialize<UserRegisteredMessage>(message)
                ?? throw new JsonException("The user-registered message body is null.");

            await createWalletHandler.HandleAsync(
                new CreateWalletCommand(
                    userRegistered.ExternalId,
                    userRegistered.CreatedAt),
                cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            logger.LogWarning(
                exception,
                "Skipping invalid user-registered Kafka message");

            // TODO: Publish the rejected message and failure details to a DLQ before returning.
            // Returning successfully allows KafkaConsumer to commit this message's offset.
        }
    }
}

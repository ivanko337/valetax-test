namespace Wallet.Infrastructure.Messaging;

using System.Text.Json;
using Common.Kafka;
using Common.Messages;
using Wallet.Application.Payouts;

public sealed class CommissionAccruedMessageHandler(
    ICommissionPayoutProcessor payoutProcessor,
    ILogger<CommissionAccruedMessageHandler> logger)
    : IKafkaMessageHandler
{
    public async Task HandleAsync(
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var commissionAccrued = JsonSerializer.Deserialize<CommissionAccruedMessage>(message)
                ?? throw new JsonException("The commission-accrued message body is null.");

            var result = await payoutProcessor.ProcessAsync(
                commissionAccrued,
                cancellationToken);

            if (result == CommissionPayoutResult.AlreadyPaid)
            {
                logger.LogInformation(
                    "Commission {CommissionId} has already been paid; skipping duplicate event",
                    commissionAccrued.CommissionId);
            }
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            logger.LogWarning(
                exception,
                "Skipping invalid commission-accrued Kafka message");

            // TODO: Publish the rejected message and failure details to a DLQ before returning.
            // Returning successfully allows KafkaConsumer to commit this message's offset.
        }
    }
}

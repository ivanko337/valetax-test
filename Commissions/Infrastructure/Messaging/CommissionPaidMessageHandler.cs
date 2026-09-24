namespace Commissions.Infrastructure.Messaging;

using System.Text.Json;
using Commissions.Application.Commissions;
using Common.Kafka;
using Common.Messages;

public sealed class CommissionPaidMessageHandler(
    ICommissionPaymentProcessor paymentProcessor,
    ILogger<CommissionPaidMessageHandler> logger)
    : IKafkaMessageHandler
{
    public async Task HandleAsync(
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var commissionPaid = JsonSerializer.Deserialize<CommissionPaidMessage>(message)
                ?? throw new JsonException("The commission-paid message body is null.");

            var result = await paymentProcessor.ProcessAsync(
                commissionPaid,
                cancellationToken);

            if (result == CommissionPaymentResult.AlreadyPaid)
            {
                logger.LogInformation(
                    "Commission {CommissionId} is already marked as paid; skipping duplicate event",
                    commissionPaid.CommissionId);
            }
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            logger.LogWarning(
                exception,
                "Skipping invalid commission-paid Kafka message");

            // TODO: Publish the rejected message and failure details to a DLQ before returning.
            // Returning successfully allows KafkaConsumer to commit this message's offset.
        }
    }
}

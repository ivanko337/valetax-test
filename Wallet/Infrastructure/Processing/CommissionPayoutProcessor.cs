namespace Wallet.Infrastructure.Processing;

using Common.Constants;
using Common.Messages;
using Common.Outbox;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Payouts;
using Wallet.Infrastructure.Persistence;

public sealed class CommissionPayoutProcessor(
    WalletDbContext dbContext,
    IOutboxWriter outbox,
    TimeProvider timeProvider,
    ILogger<CommissionPayoutProcessor> logger)
    : ICommissionPayoutProcessor
{
    public Task<CommissionPayoutResult> ProcessAsync(
        CommissionAccruedMessage message,
        CancellationToken cancellationToken)
    {
        Validate(message);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            var paidAt = timeProvider.GetUtcNow().ToUniversalTime();

            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Wallets" ("UserExternalId", "BalanceCents", "CreatedAt")
                VALUES ({message.UserExternalId}, 0, {paidAt})
                ON CONFLICT ("UserExternalId") DO NOTHING
                """, cancellationToken);

            var insertedPayouts = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Payouts" ("CommissionId", "UserExternalId", "AmountCents", "PaidAt")
                VALUES ({message.CommissionId}, {message.UserExternalId}, {message.AmountCents}, {paidAt})
                ON CONFLICT ("CommissionId") DO NOTHING
                """, cancellationToken);

            if (insertedPayouts == 0)
            {
                // Roll back the wallet upsert too. A conflicting duplicate must have no
                // side effects, even if it carries a different user ID.
                await transaction.RollbackAsync(cancellationToken);
                logger.LogDebug(
                    "Commission {CommissionId} has already been paid; skipping duplicate event",
                    message.CommissionId);
                return CommissionPayoutResult.AlreadyPaid;
            }

            await dbContext.Wallets
                .Where(wallet => wallet.UserExternalId == message.UserExternalId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        wallet => wallet.BalanceCents,
                        wallet => wallet.BalanceCents + message.AmountCents),
                    cancellationToken);

            outbox.Add(
                KafkaConstants.CommissionsPaidTopic,
                new CommissionPaidMessage(
                    message.CommissionId,
                    message.UserExternalId,
                    message.AmountCents,
                    paidAt),
                message.UserExternalId.ToString("D"));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Recorded payout for commission {CommissionId}: {AmountCents} cents to user {UserExternalId}",
                message.CommissionId,
                message.AmountCents,
                message.UserExternalId);

            return CommissionPayoutResult.Paid;
        });
    }

    private static void Validate(CommissionAccruedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.CommissionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Commission ID must not be empty.",
                nameof(message));
        }

        if (message.UserExternalId == Guid.Empty)
        {
            throw new ArgumentException(
                "User external ID must not be empty.",
                nameof(message));
        }
    }
}

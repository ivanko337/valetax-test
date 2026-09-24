using Commissions.Application.PartnerGraph;
using Commissions.Domain.Commissions;
using Commissions.Domain.ProfitEvents;
using Commissions.Infrastructure.Persistence;
using Commissions.Infrastructure.Processing.Models;
using Common.Constants;
using Common.Messages;
using Common.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Commissions.Infrastructure.Processing;

internal sealed class ProfitEventProcessor(
    CommissionsDbContext dbContext,
    IPartnerGraphClient partnerGraphClient,
    IOutboxWriter outbox,
    TimeProvider timeProvider,
    IOptions<ProfitEventProcessingOptions> options,
    ILogger<ProfitEventProcessor> logger)
{
    private readonly ProfitEventProcessingOptions _options = options.Value;

    public async Task ProcessAsync(
        ProfitEventWorkItem workItem,
        CancellationToken cancellationToken)
    {
        try
        {
            var scheme = await dbContext.CommissionSchemes
                .AsNoTracking()
                .OrderByDescending(item => item.Version)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "No commission calculation scheme is configured.");

            CalculatedCommission[] calculations = [];
            if (workItem.ProfitCents > 0)
            {
                var partners = await partnerGraphClient.GetUplinePartnersAsync(
                    workItem.UserExternalId,
                    cancellationToken);

                calculations = partners
                    .Select(partner => new CalculatedCommission(
                        Guid.NewGuid(),
                        partner.ExternalUserId,
                        partner.Level,
                        CommissionCalculator.CalculateAmountCents(
                            workItem.ProfitCents,
                            partner.Level,
                            scheme.SchemaType)))
                    .ToArray();
            }

            var calculatedAt = timeProvider.GetUtcNow();
            var completed = await CompleteClaimAsync(
                workItem,
                scheme.Version,
                scheme.SchemaType,
                calculatedAt,
                calculations,
                cancellationToken);

            if (!completed)
            {
                logger.LogWarning(
                    "Discarded calculated result for profit event {ExternalEventId} because its claim is no longer owned",
                    workItem.ExternalEventId);
            }
            else
            {
                logger.LogDebug(
                    "Calculated profit event {ExternalEventId} with scheme {SchemaType} version {SchemaVersion}; created {CommissionCount} commissions",
                    workItem.ExternalEventId,
                    scheme.SchemaType,
                    scheme.Version,
                    calculations.Length);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var maxAttempts = _options.MaxRetryCount + 1;
            var retriesExhausted = workItem.CalculationAttempts >= maxAttempts;

            await RegisterFailureAsync(
                workItem,
                retriesExhausted,
                cancellationToken);

            if (retriesExhausted)
            {
                logger.LogError(
                    exception,
                    "Profit event {ExternalEventId} failed after {CalculationAttempts} attempts",
                    workItem.ExternalEventId,
                    workItem.CalculationAttempts);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Profit event {ExternalEventId} calculation attempt {CalculationAttempt} failed; it will be retried",
                    workItem.ExternalEventId,
                    workItem.CalculationAttempts);
            }
        }

    }

    private Task<bool> CompleteClaimAsync(
        ProfitEventWorkItem workItem,
        int schemaVersion,
        Common.Enums.CommissionSchemaType schemaType,
        DateTimeOffset calculatedAt,
        IReadOnlyCollection<CalculatedCommission> calculations,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            var profitEvent = await LockClaimedEventAsync(workItem, cancellationToken);
            if (profitEvent is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            foreach (var calculation in calculations)
            {
                var commission = new Commission(
                    calculation.Id,
                    workItem.ExternalEventId,
                    calculation.BeneficiaryId,
                    calculation.AmountCents,
                    schemaVersion,
                    calculation.Level);

                dbContext.Commissions.Add(commission);
                outbox.Add(
                    KafkaConstants.CommissionsAccruedTopic,
                    new CommissionAccruedMessage(
                        commission.Id,
                        commission.ExternalEventId,
                        commission.BeneficiaryId,
                        commission.AmountCents,
                        commission.Level,
                        commission.SchemaVersion,
                        calculatedAt),
                    commission.BeneficiaryId.ToString("D"));
            }

            profitEvent.CompleteCalculation(schemaType, calculatedAt);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    private Task RegisterFailureAsync(
        ProfitEventWorkItem workItem,
        bool retriesExhausted,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            // A failed completion can leave several tracked commissions and outbox rows.
            // Clear the whole failed unit of work before recording only the retry state.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            var profitEvent = await LockClaimedEventAsync(workItem, cancellationToken);
            if (profitEvent is not null)
            {
                profitEvent.RegisterCalculationFailure(
                    retriesExhausted,
                    timeProvider.GetUtcNow() + _options.RetryDelay);

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private Task<ProfitEvent?> LockClaimedEventAsync(
        ProfitEventWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var calculatingStatus = (short)ProfitEventStatus.Calculating;

        return dbContext.ProfitEvents
            .FromSqlInterpolated($"""
                SELECT *
                FROM "ProfitEvents"
                WHERE "ExternalEventId" = {workItem.ExternalEventId}
                    AND "Status" = {calculatingStatus}
                    AND "CalculationToken" = {workItem.CalculationToken}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }
}

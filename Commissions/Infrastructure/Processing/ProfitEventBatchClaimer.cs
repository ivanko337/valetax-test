using Commissions.Domain.ProfitEvents;
using Commissions.Infrastructure.Persistence;
using Commissions.Infrastructure.Processing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Commissions.Infrastructure.Processing;

internal sealed class ProfitEventBatchClaimer(
    CommissionsDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<ProfitEventProcessingOptions> options)
{
    private readonly ProfitEventProcessingOptions _options = options.Value;

    public Task<IReadOnlyList<ProfitEventWorkItem>> ClaimBatchAsync(
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync<IReadOnlyList<ProfitEventWorkItem>>(async () =>
        {
            // The first execution starts with an empty tracker because the claimer has
            // its own scope. A transient failure can rerun this delegate on the same
            // DbContext, so clear state left by the previous execution-strategy attempt.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            var now = timeProvider.GetUtcNow();
            var staleBefore = now - _options.ClaimTimeout;
            var maxAttempts = _options.MaxRetryCount + 1;
            var pendingStatus = (short)ProfitEventStatus.PendingCalculation;
            var calculatingStatus = (short)ProfitEventStatus.Calculating;

            var profitEvents = await dbContext.ProfitEvents
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "ProfitEvents"
                    WHERE
                        (
                            "Status" = {pendingStatus}
                            AND "CalculationAttempts" < {maxAttempts}
                            AND (
                                "NextCalculationAttemptAt" IS NULL
                                OR "NextCalculationAttemptAt" <= {now}
                            )
                        )
                        OR (
                            "Status" = {calculatingStatus}
                            AND "CalculationAttempts" <= {maxAttempts}
                            AND "CalculationStartedAt" <= {staleBefore}
                        )
                    ORDER BY "ReceivedAt", "ExternalEventId"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {_options.BatchSize}
                    """)
                .ToListAsync(cancellationToken);

            var workItems = new List<ProfitEventWorkItem>(profitEvents.Count);

            foreach (var profitEvent in profitEvents)
            {
                var calculationToken = Guid.NewGuid();

                if (profitEvent.Status == ProfitEventStatus.PendingCalculation)
                {
                    profitEvent.StartCalculation(calculationToken, now);
                }
                else
                {
                    profitEvent.ReclaimCalculation(calculationToken, now);
                }

                workItems.Add(new ProfitEventWorkItem(
                    profitEvent.ExternalEventId,
                    profitEvent.UserExternalId,
                    profitEvent.ProfitCents,
                    profitEvent.CalculationAttempts,
                    calculationToken));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return workItems;
        });
    }
}

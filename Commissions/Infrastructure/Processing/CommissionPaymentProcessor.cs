namespace Commissions.Infrastructure.Processing;

using Commissions.Application.Commissions;
using Commissions.Domain.Commissions;
using Commissions.Infrastructure.Persistence;
using Common.Messages;
using Microsoft.EntityFrameworkCore;

public sealed class CommissionPaymentProcessor(CommissionsDbContext dbContext)
    : ICommissionPaymentProcessor
{
    public Task<CommissionPaymentResult> ProcessAsync(
        CommissionPaidMessage message,
        CancellationToken cancellationToken)
    {
        Validate(message);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            var paidAt = message.PaidAt.ToUniversalTime();
            var updatedRows = await dbContext.Commissions
                .Where(commission =>
                    commission.Id == message.CommissionId
                    && commission.BeneficiaryId == message.UserExternalId
                    && commission.AmountCents == message.AmountCents
                    && commission.PaymentStatus == CommissionPaymentStatus.Pending)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            commission => commission.PaymentStatus,
                            CommissionPaymentStatus.Paid)
                        .SetProperty(
                            commission => commission.PaidAt,
                            paidAt),
                    cancellationToken);

            if (updatedRows == 1)
            {
                return CommissionPaymentResult.Paid;
            }

            var commission = await dbContext.Commissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == message.CommissionId,
                    cancellationToken)
                ?? throw new ArgumentException(
                    $"Commission '{message.CommissionId}' does not exist.",
                    nameof(message));

            if (commission.BeneficiaryId != message.UserExternalId
                || commission.AmountCents != message.AmountCents)
            {
                throw new ArgumentException(
                    "The commission-paid event does not match the stored commission.",
                    nameof(message));
            }

            if (commission.PaymentStatus == CommissionPaymentStatus.Paid)
            {
                return CommissionPaymentResult.AlreadyPaid;
            }

            throw new InvalidOperationException(
                $"Commission '{message.CommissionId}' could not be marked as paid.");
        });
    }

    private static void Validate(CommissionPaidMessage message)
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

        if (message.PaidAt == default)
        {
            throw new ArgumentException(
                "Paid timestamp must not be empty.",
                nameof(message));
        }
    }
}

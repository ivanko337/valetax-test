namespace Commissions.Application.Commissions;

using Common.Messages;

public interface ICommissionPaymentProcessor
{
    Task<CommissionPaymentResult> ProcessAsync(
        CommissionPaidMessage message,
        CancellationToken cancellationToken);
}

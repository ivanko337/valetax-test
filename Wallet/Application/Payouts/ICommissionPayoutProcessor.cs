namespace Wallet.Application.Payouts;

using Common.Messages;

public interface ICommissionPayoutProcessor
{
    Task<CommissionPayoutResult> ProcessAsync(
        CommissionAccruedMessage message,
        CancellationToken cancellationToken);
}

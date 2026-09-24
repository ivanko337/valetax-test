namespace Commissions.Domain.ProfitEvents;

public enum ProfitEventStatus : byte
{
    PendingCalculation = 0,
    Calculated = 1,
    Failed = 2,
    Calculating = 3
}

namespace Commissions.Infrastructure.Processing.Models;

internal sealed record CalculatedCommission(
    Guid Id,
    Guid BeneficiaryId,
    int Level,
    long AmountCents);

using Common.Enums;

namespace Commissions.Domain.ProfitEvents;

public sealed class ProfitEvent(
    Guid externalEventId,
    Guid userExternalId,
    long profitCents,
    DateTimeOffset ocurredAt,
    DateTimeOffset receivedAt)
{
    public Guid ExternalEventId { get; private set; } = externalEventId;

    public Guid UserExternalId { get; private set; } = userExternalId;

    public long ProfitCents { get; private set; } = profitCents;

    public DateTimeOffset OcurredAt { get; private set; } = ocurredAt;

    public DateTimeOffset ReceivedAt { get; private set; } = receivedAt;

    public ProfitEventStatus Status { get; private set; } = ProfitEventStatus.PendingCalculation;

    public CommissionSchemaType? SchemaType { get; private set; }

    public DateTimeOffset? CalculatedAt { get; private set; }

    public int CalculationAttempts { get; private set; }

    public DateTimeOffset? NextCalculationAttemptAt { get; private set; }

    public DateTimeOffset? CalculationStartedAt { get; private set; }

    public Guid? CalculationToken { get; private set; }

    public bool HasSamePayload(
        Guid otherUserExternalId,
        long otherProfitCents,
        DateTimeOffset otherOcurredAt)
    {
        return UserExternalId == otherUserExternalId
            && ProfitCents == otherProfitCents
            && OcurredAt == otherOcurredAt;
    }

    public void StartCalculation(Guid calculationToken, DateTimeOffset startedAt)
    {
        if (Status != ProfitEventStatus.PendingCalculation)
        {
            throw new InvalidOperationException(
                $"A profit event in status '{Status}' cannot start calculation.");
        }

        CalculationAttempts++;
        SetCalculationClaim(calculationToken, startedAt);
    }

    public void ReclaimCalculation(Guid calculationToken, DateTimeOffset startedAt)
    {
        if (Status != ProfitEventStatus.Calculating)
        {
            throw new InvalidOperationException(
                $"A profit event in status '{Status}' cannot be reclaimed.");
        }

        SetCalculationClaim(calculationToken, startedAt);
    }

    public void CompleteCalculation(
        CommissionSchemaType schemaType,
        DateTimeOffset calculatedAt)
    {
        EnsureCalculating();

        Status = ProfitEventStatus.Calculated;
        SchemaType = schemaType;
        CalculatedAt = calculatedAt;
        CalculationStartedAt = null;
        CalculationToken = null;
        NextCalculationAttemptAt = null;
    }

    public void RegisterCalculationFailure(
        bool retriesExhausted,
        DateTimeOffset nextAttemptAt)
    {
        EnsureCalculating();

        Status = retriesExhausted
            ? ProfitEventStatus.Failed
            : ProfitEventStatus.PendingCalculation;
        CalculationStartedAt = null;
        CalculationToken = null;
        NextCalculationAttemptAt = retriesExhausted ? null : nextAttemptAt;
    }

    private void EnsureCalculating()
    {
        if (Status != ProfitEventStatus.Calculating)
        {
            throw new InvalidOperationException(
                $"A profit event in status '{Status}' is not being calculated.");
        }
    }

    private void SetCalculationClaim(
        Guid calculationToken,
        DateTimeOffset startedAt)
    {
        if (calculationToken == Guid.Empty)
        {
            throw new ArgumentException(
                "Calculation token must not be empty.",
                nameof(calculationToken));
        }

        Status = ProfitEventStatus.Calculating;
        CalculationStartedAt = startedAt;
        CalculationToken = calculationToken;
        NextCalculationAttemptAt = null;
    }
}

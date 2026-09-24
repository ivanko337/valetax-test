using Commissions.Domain.ProfitEvents;
using Common.Enums;
using Xunit;

namespace Commissions.Tests.Domain.ProfitEvents;

public sealed class ProfitEventTests
{
    [Fact]
    public void CalculationLifecycle_TracksAttemptsRetriesAndCompletion()
    {
        var profitEvent = CreateProfitEvent();
        var firstToken = Guid.NewGuid();

        profitEvent.StartCalculation(firstToken, Utc(12));

        Assert.Equal(ProfitEventStatus.Calculating, profitEvent.Status);
        Assert.Equal(1, profitEvent.CalculationAttempts);
        Assert.Equal(firstToken, profitEvent.CalculationToken);

        profitEvent.RegisterCalculationFailure(
            retriesExhausted: false,
            nextAttemptAt: Utc(13));

        Assert.Equal(ProfitEventStatus.PendingCalculation, profitEvent.Status);
        Assert.Equal(Utc(13), profitEvent.NextCalculationAttemptAt);

        profitEvent.StartCalculation(Guid.NewGuid(), Utc(13));
        profitEvent.CompleteCalculation(CommissionSchemaType.Fibonacci, Utc(14));

        Assert.Equal(ProfitEventStatus.Calculated, profitEvent.Status);
        Assert.Equal(2, profitEvent.CalculationAttempts);
        Assert.Equal(CommissionSchemaType.Fibonacci, profitEvent.SchemaType);
        Assert.Equal(Utc(14), profitEvent.CalculatedAt);
        Assert.Null(profitEvent.CalculationToken);
    }

    [Fact]
    public void StartCalculation_RejectsAlreadyCalculatingEvent()
    {
        var profitEvent = CreateProfitEvent();

        profitEvent.StartCalculation(Guid.NewGuid(), Utc(12));

        Assert.Throws<InvalidOperationException>(() =>
            profitEvent.StartCalculation(Guid.NewGuid(), Utc(13)));

        Assert.Equal(1, profitEvent.CalculationAttempts);
    }

    [Fact]
    public void ReclaimCalculation_ReplacesStaleClaimWithoutCountingAnotherAttempt()
    {
        var profitEvent = CreateProfitEvent();
        profitEvent.StartCalculation(Guid.NewGuid(), Utc(12));
        var replacementToken = Guid.NewGuid();

        profitEvent.ReclaimCalculation(replacementToken, Utc(13));

        Assert.Equal(1, profitEvent.CalculationAttempts);
        Assert.Equal(replacementToken, profitEvent.CalculationToken);
        Assert.Equal(Utc(13), profitEvent.CalculationStartedAt);
    }

    [Fact]
    public void RegisterCalculationFailure_MarksEventFailedWhenRetriesAreExhausted()
    {
        var profitEvent = CreateProfitEvent();
        profitEvent.StartCalculation(Guid.NewGuid(), Utc(12));

        profitEvent.RegisterCalculationFailure(
            retriesExhausted: true,
            nextAttemptAt: Utc(13));

        Assert.Equal(ProfitEventStatus.Failed, profitEvent.Status);
        Assert.Null(profitEvent.NextCalculationAttemptAt);
        Assert.Null(profitEvent.CalculationToken);
    }

    private static ProfitEvent CreateProfitEvent()
    {
        return new ProfitEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            10_000,
            Utc(10),
            Utc(11));
    }

    private static DateTimeOffset Utc(int hour)
    {
        return new DateTimeOffset(2026, 9, 24, hour, 0, 0, TimeSpan.Zero);
    }
}

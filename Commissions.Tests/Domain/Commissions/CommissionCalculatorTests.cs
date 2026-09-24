using Commissions.Domain.Commissions;
using Common.Enums;
using Xunit;

namespace Commissions.Tests.Domain.Commissions;

public sealed class CommissionCalculatorTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(10, 1_000)]
    public void CalculateAmountCents_UsesLinearLevelAsPercentage(
        int level,
        long expectedAmountCents)
    {
        var result = CommissionCalculator.CalculateAmountCents(
            profitCents: 10_000,
            level,
            CommissionSchemaType.Linear);

        Assert.Equal(expectedAmountCents, result);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 100)]
    [InlineData(3, 200)]
    [InlineData(6, 800)]
    public void CalculateAmountCents_UsesOneBasedFibonacciLevel(
        int level,
        long expectedAmountCents)
    {
        var result = CommissionCalculator.CalculateAmountCents(
            profitCents: 10_000,
            level,
            CommissionSchemaType.Fibonacci);

        Assert.Equal(expectedAmountCents, result);
    }

    [Fact]
    public void CalculateAmountCents_UsesInt128ForIntermediateMultiplication()
    {
        var result = CommissionCalculator.CalculateAmountCents(
            long.MaxValue,
            level: 100,
            CommissionSchemaType.Linear);

        Assert.Equal(long.MaxValue, result);
    }

    [Theory]
    [InlineData(CommissionSchemaType.Linear, 2, -200)]
    [InlineData(CommissionSchemaType.Fibonacci, 6, -800)]
    public void CalculateAmountCents_PreservesNegativeProfit(
        CommissionSchemaType schemaType,
        int level,
        long expectedAmountCents)
    {
        var result = CommissionCalculator.CalculateAmountCents(
            profitCents: -10_000,
            level,
            schemaType);

        Assert.Equal(expectedAmountCents, result);
    }

    [Fact]
    public void CalculateAmountCents_RejectsResultOutsideInt64Range()
    {
        Assert.Throws<OverflowException>(() =>
            CommissionCalculator.CalculateAmountCents(
                long.MaxValue,
                level: 101,
                CommissionSchemaType.Linear));
    }

    [Fact]
    public void CalculateAmountCents_RejectsNonPositiveLevel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommissionCalculator.CalculateAmountCents(
                profitCents: 10_000,
                level: 0,
                CommissionSchemaType.Linear));
    }
}

using Common.Enums;

namespace Commissions.Domain.Commissions;

public static class CommissionCalculator
{
    public static long CalculateAmountCents(
        long profitCents,
        int level,
        CommissionSchemaType schemaType)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                "Partner level must be positive.");
        }

        var multiplier = schemaType switch
        {
            CommissionSchemaType.Linear => (Int128)level,
            CommissionSchemaType.Fibonacci => Fibonacci(level),
            _ => throw new ArgumentOutOfRangeException(
                nameof(schemaType),
                schemaType,
                "Unsupported commission schema type.")
        };

        var amount = checked(multiplier * (Int128)profitCents) / 100;
        return checked((long)amount);
    }

    private static Int128 Fibonacci(int level)
    {
        // Partnership levels use F(1) = 1, F(2) = 1, F(3) = 2, and so on.
        if (level <= 2)
        {
            return 1;
        }

        Int128 previous = 1;
        Int128 current = 1;

        for (var index = 3; index <= level; index++)
        {
            var next = checked(previous + current);
            previous = current;
            current = next;
        }

        return current;
    }
}

namespace Commissions.Infrastructure.Processing;

public sealed class ProfitEventProcessingOptions
{
    public const string SectionName = "ProfitEventProcessing";

    public int MaxRetryCount { get; init; } = 10;

    public int BatchSize { get; init; } = 20;

    public int MaxDegreeOfParallelism { get; init; } = 4;

    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan ClaimTimeout { get; init; } = TimeSpan.FromMinutes(5);
}

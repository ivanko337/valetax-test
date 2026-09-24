namespace Common.Kafka;

public sealed class KafkaPublisherOptions
{
    public required string BootstrapServers { get; init; }

    public string? ClientId { get; init; }

    public IReadOnlyDictionary<string, string>? AdditionalConfig { get; init; }
}

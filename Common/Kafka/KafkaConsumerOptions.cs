namespace Common.Kafka;

public sealed class KafkaConsumerOptions
{
    public required string BootstrapServers { get; init; }

    public required string GroupId { get; init; }

    public string? ClientId { get; init; }

    public IReadOnlyDictionary<string, string>? AdditionalConfig { get; init; }
}

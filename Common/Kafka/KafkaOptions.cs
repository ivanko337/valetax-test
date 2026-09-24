namespace Common.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public string? ClientId { get; set; }

    public Dictionary<string, string> CommonConfig { get; set; } = [];

    public Dictionary<string, string> ProducerConfig { get; set; } = [];

    public Dictionary<string, string> ConsumerConfig { get; set; } = [];

    public KafkaTopicInitializationOptions TopicInitialization { get; set; } = new();
}

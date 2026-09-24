namespace Common.Kafka;

public sealed class KafkaTopicInitializationOptions
{
    public bool Enabled { get; set; } = true;

    public int NumPartitions { get; set; } = 1;

    public short ReplicationFactor { get; set; } = 1;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

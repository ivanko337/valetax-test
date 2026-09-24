using Common.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Common.Tests.Kafka;

public sealed class KafkaRegistrationTests
{
    [Fact]
    public void AddKafka_WithZeroTopicPartitions_Throws()
    {
        var options = CreateOptions();
        options.TopicInitialization.NumPartitions = 0;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ServiceCollection().AddKafka(options));
    }

    [Fact]
    public void AddKafka_WithZeroTopicReplicationFactor_Throws()
    {
        var options = CreateOptions();
        options.TopicInitialization.ReplicationFactor = 0;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ServiceCollection().AddKafka(options));
    }

    [Fact]
    public void AddKafka_WithZeroTopicRequestTimeout_Throws()
    {
        var options = CreateOptions();
        options.TopicInitialization.RequestTimeout = TimeSpan.Zero;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ServiceCollection().AddKafka(options));
    }

    [Fact]
    public void AddKafka_WhenTopicInitializationIsDisabled_IgnoresItsCreationSettings()
    {
        var options = CreateOptions();
        options.TopicInitialization = new()
        {
            Enabled = false,
            NumPartitions = 0,
            ReplicationFactor = 0,
            RequestTimeout = TimeSpan.Zero
        };

        var exception = Record.Exception(
            () => new ServiceCollection().AddKafka(options));

        Assert.Null(exception);
    }

    private static KafkaOptions CreateOptions()
    {
        return new()
        {
            BootstrapServers = "localhost:9092",
            GroupId = "tests"
        };
    }
}

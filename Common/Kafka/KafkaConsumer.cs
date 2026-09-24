using Confluent.Kafka;

namespace Common.Kafka;

public sealed class KafkaConsumer : IKafkaConsumer
{
    private readonly IConsumer<Ignore, string> _consumer;

    public KafkaConsumer(KafkaConsumerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.BootstrapServers);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GroupId);

        var config = new ConsumerConfig();

        if (options.AdditionalConfig is not null)
        {
            foreach (var (key, value) in options.AdditionalConfig)
            {
                config.Set(key, value);
            }
        }

        config.BootstrapServers = options.BootstrapServers;
        config.GroupId = options.GroupId;
        config.ClientId = options.ClientId;
        config.EnableAutoCommit = false;
        config.EnableAutoOffsetStore = false;

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
    }

    public async Task ConsumeAsync(
        IEnumerable<string> topics,
        Func<string, CancellationToken, Task> messageHandler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentNullException.ThrowIfNull(messageHandler);

        var topicList = topics
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (topicList.Length == 0)
        {
            throw new ArgumentException("At least one topic is required.", nameof(topics));
        }

        _consumer.Subscribe(topicList);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string> result;

                try
                {
                    result = _consumer.Consume(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                using var activity = KafkaTraceContext.StartConsumerActivity(
                    result.Topic,
                    result.Message.Headers);

                await messageHandler(result.Message.Value, cancellationToken)
                    .ConfigureAwait(false);

                _consumer.Commit(result);
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }
}

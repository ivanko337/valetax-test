using System.Diagnostics;
using Confluent.Kafka;

namespace Common.Kafka;

public sealed class KafkaPublisher : IKafkaPublisher
{
    private readonly IProducer<string?, string> _producer;

    public KafkaPublisher(KafkaPublisherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.BootstrapServers);

        var config = new ProducerConfig();

        if (options.AdditionalConfig is not null)
        {
            foreach (var (key, value) in options.AdditionalConfig)
            {
                config.Set(key, value);
            }
        }

        config.BootstrapServers = options.BootstrapServers;
        config.ClientId = options.ClientId;

        _producer = new ProducerBuilder<string?, string>(config).Build();
    }

    public async Task PublishAsync(
        string topic,
        string message,
        CancellationToken cancellationToken = default)
    {
        await PublishAsync(
                topic,
                message,
                key: null,
                headers: null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task PublishAsync(
        string topic,
        string message,
        string? key,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(message);

        using var activity = KafkaTraceContext.StartProducerActivity(topic, headers);

        var kafkaHeaders = KafkaTraceContext.CreateHeaders(activity);

        if (headers is not null)
        {
            foreach (var (name, value) in headers)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);
                ArgumentNullException.ThrowIfNull(value);
                if (!IsTraceHeader(name))
                {
                    kafkaHeaders.Add(name, System.Text.Encoding.UTF8.GetBytes(value));
                }
            }
        }

        var kafkaMessage = new Message<string?, string>
        {
            Key = key,
            Value = message,
            Headers = kafkaHeaders
        };

        await _producer
            .ProduceAsync(topic, kafkaMessage, cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsTraceHeader(string name)
    {
        return string.Equals(
                name,
                KafkaTraceContext.TraceParentHeaderName,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                name,
                KafkaTraceContext.TraceStateHeaderName,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                name,
                KafkaTraceContext.TraceIdHeaderName,
                StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}

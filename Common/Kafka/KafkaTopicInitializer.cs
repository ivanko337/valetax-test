using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace Common.Kafka;

internal sealed class KafkaTopicInitializer(
    KafkaTopicInitializerSettings settings,
    ILogger<KafkaTopicInitializer> logger)
{
    public async Task InitializeAsync(
        IEnumerable<string> topics,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(topics);

        if (!settings.Enabled)
        {
            logger.LogInformation("Kafka topic initialization is disabled");
            return;
        }

        var topicList = topics.ToArray();

        if (topicList.Length == 0)
        {
            throw new ArgumentException("At least one topic is required.", nameof(topics));
        }

        if (topicList.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Topic names cannot be empty.", nameof(topics));
        }

        var specifications = topicList
            .Distinct(StringComparer.Ordinal)
            .Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = settings.NumPartitions,
                ReplicationFactor = settings.ReplicationFactor
            })
            .ToArray();

        using var adminClient = CreateAdminClient();

        try
        {
            await adminClient
                .CreateTopicsAsync(
                    specifications,
                    new CreateTopicsOptions
                    {
                        OperationTimeout = settings.RequestTimeout,
                        RequestTimeout = settings.RequestTimeout
                    })
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Created Kafka topics: {Topics}",
                string.Join(", ", specifications.Select(specification => specification.Name)));
        }
        catch (CreateTopicsException exception)
            when (exception.Results.All(result => IsSuccessfulOrAlreadyExists(result.Error.Code)))
        {
            logger.LogInformation(
                "Kafka topics are initialized: {Topics}",
                string.Join(", ", specifications.Select(specification => specification.Name)));
        }
    }

    private IAdminClient CreateAdminClient()
    {
        var config = new AdminClientConfig();

        if (settings.AdditionalConfig is not null)
        {
            foreach (var (key, value) in settings.AdditionalConfig)
            {
                config.Set(key, value);
            }
        }

        config.BootstrapServers = settings.BootstrapServers;
        config.ClientId = settings.ClientId;

        return new AdminClientBuilder(config).Build();
    }

    private static bool IsSuccessfulOrAlreadyExists(ErrorCode errorCode)
    {
        return errorCode is ErrorCode.NoError or ErrorCode.TopicAlreadyExists;
    }
}

internal sealed record KafkaTopicInitializerSettings(
    bool Enabled,
    string BootstrapServers,
    string? ClientId,
    int NumPartitions,
    short ReplicationFactor,
    TimeSpan RequestTimeout,
    IReadOnlyDictionary<string, string>? AdditionalConfig);

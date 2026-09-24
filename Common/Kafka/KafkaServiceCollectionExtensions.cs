using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Kafka;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafka(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Kafka")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = configuration.GetRequiredSection(sectionName);
        var options = section.Get<KafkaOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{sectionName}' could not be bound to Kafka options.");

        return services.AddKafka(options);
    }

    public static IServiceCollection AddKafka(
        this IServiceCollection services,
        KafkaOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.BootstrapServers);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GroupId);

        var topicInitialization = options.TopicInitialization
            ?? throw new ArgumentException(
                "Kafka topic initialization options are required.",
                nameof(options));

        if (topicInitialization.Enabled)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                topicInitialization.NumPartitions);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                topicInitialization.ReplicationFactor);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
                topicInitialization.RequestTimeout,
                TimeSpan.Zero);
        }

        var publisherOptions = new KafkaPublisherOptions
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            AdditionalConfig = MergeConfig(options.CommonConfig, options.ProducerConfig)
        };

        var consumerOptions = new KafkaConsumerOptions
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            ClientId = options.ClientId,
            AdditionalConfig = MergeConfig(options.CommonConfig, options.ConsumerConfig)
        };

        services.AddSingleton<IKafkaPublisher>(
            _ => new KafkaPublisher(publisherOptions));
        services.AddTransient<IKafkaConsumer>(
            _ => new KafkaConsumer(consumerOptions));

        var initializerSettings = new KafkaTopicInitializerSettings(
            topicInitialization.Enabled,
            options.BootstrapServers,
            options.ClientId,
            topicInitialization.NumPartitions,
            topicInitialization.ReplicationFactor,
            topicInitialization.RequestTimeout,
            MergeConfig(options.CommonConfig, specificConfig: null));

        services.AddSingleton(serviceProvider =>
            new KafkaTopicInitializer(
                initializerSettings,
                serviceProvider.GetRequiredService<
                    Microsoft.Extensions.Logging.ILogger<KafkaTopicInitializer>>()));

        return services;
    }

    public static IServiceCollection AddKafkaConsumer<THandler>(
        this IServiceCollection services,
        params string[] topics)
        where THandler : class, IKafkaMessageHandler
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(topics);

        var topicList = topics
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (topicList.Length == 0)
        {
            throw new ArgumentException(
                "At least one topic is required.",
                nameof(topics));
        }

        services.AddScoped<THandler>();
        services.AddSingleton<IHostedService>(serviceProvider =>
            new KafkaConsumerHostedService<THandler>(
                serviceProvider.GetRequiredService<IKafkaConsumer>(),
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                topicList));

        return services;
    }

    private static IReadOnlyDictionary<string, string>? MergeConfig(
        IReadOnlyDictionary<string, string>? commonConfig,
        IReadOnlyDictionary<string, string>? specificConfig)
    {
        if ((commonConfig is null || commonConfig.Count == 0)
            && (specificConfig is null || specificConfig.Count == 0))
        {
            return null;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (commonConfig is not null)
        {
            foreach (var (key, value) in commonConfig)
            {
                result[key] = value;
            }
        }

        if (specificConfig is not null)
        {
            foreach (var (key, value) in specificConfig)
            {
                result[key] = value;
            }
        }

        return result;
    }
}

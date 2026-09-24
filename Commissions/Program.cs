namespace Commissions;

using Commissions.Application.Commissions;
using Commissions.Application.CommissionSchemes;
using Commissions.Application.PartnerGraph;
using Commissions.Application.ProfitEvents;
using Commissions.Infrastructure.Messaging;
using Commissions.Infrastructure.PartnerGraph;
using Commissions.Infrastructure.Persistence;
using Commissions.Infrastructure.Persistence.Repositories;
using Commissions.Infrastructure.Processing;
using Common.Application;
using Common.Constants;
using Common.Kafka;
using Common.Observability;
using Common.Outbox;
using Common.Persistence;
using PartnerGraph.Grpc;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceObservability();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new() { Title = "Commissions API", Version = "v1" }));
        builder.Services.AddPostgreSqlDbContext<CommissionsDbContext>(builder.Configuration);
        builder.Services.AddKafka(builder.Configuration);
        builder.Services.AddKafkaConsumer<CommissionPaidMessageHandler>(
            KafkaConstants.CommissionsPaidTopic);
        builder.Services.AddOutbox<CommissionsDbContext, KafkaMessageTransport>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IProfitEventRepository, ProfitEventRepository>();
        builder.Services.AddScoped<IProfitEventDetailsReader, ProfitEventDetailsReader>();
        builder.Services.AddScoped<IProfitEventReader, ProfitEventReader>();
        builder.Services.AddScoped<ICommissionSchemeRepository, CommissionSchemeRepository>();
        builder.Services.AddScoped<ICommissionPaymentProcessor, CommissionPaymentProcessor>();
        builder.Services.AddScoped<
            ICommandHandler<ReceiveProfitEventCommand, ReceiveProfitEventResult>,
            ReceiveProfitEventHandler>();
        builder.Services.AddScoped<
            IQueryHandler<GetProfitEventDetailsQuery, ProfitEventDetails?>,
            GetProfitEventDetailsHandler>();
        builder.Services.AddScoped<
            IQueryHandler<GetUserProfitEventsQuery, IReadOnlyList<ProfitEventListItem>>,
            GetUserProfitEventsHandler>();
        builder.Services.AddScoped<
            ICommandHandler<SwitchCommissionSchemeCommand, CommissionSchemeResult>,
            SwitchCommissionSchemeHandler>();
        builder.Services.AddScoped<IPartnerGraphClient, PartnerGraphClient>();
        builder.Services.AddScoped<ProfitEventBatchClaimer>();
        builder.Services.AddScoped<ProfitEventProcessor>();
        builder.Services.AddHostedService<ProfitEventWorker>();
        builder.Services
            .AddOptions<ProfitEventProcessingOptions>()
            .BindConfiguration(ProfitEventProcessingOptions.SectionName)
            .Validate(
                options => options.MaxRetryCount is >= 0 and <= 100,
                "ProfitEventProcessing:MaxRetryCount must be between 0 and 100.")
            .Validate(
                options => options.BatchSize is >= 1 and <= 1000,
                "ProfitEventProcessing:BatchSize must be between 1 and 1000.")
            .Validate(
                options => options.MaxDegreeOfParallelism is >= 1 and <= 100,
                "ProfitEventProcessing:MaxDegreeOfParallelism must be between 1 and 100.")
            .Validate(
                options => options.MaxDegreeOfParallelism <= options.BatchSize,
                "ProfitEventProcessing:MaxDegreeOfParallelism must not exceed BatchSize.")
            .Validate(
                options => options.PollingInterval > TimeSpan.Zero,
                "ProfitEventProcessing:PollingInterval must be positive.")
            .Validate(
                options => options.RetryDelay > TimeSpan.Zero,
                "ProfitEventProcessing:RetryDelay must be positive.")
            .Validate(
                options => options.ClaimTimeout > TimeSpan.Zero,
                "ProfitEventProcessing:ClaimTimeout must be positive.")
            .ValidateOnStart();

        var partnerGraphGrpcAddress = builder.Configuration["PartnerGraphGrpc:Address"]
            ?? throw new InvalidOperationException("PartnerGraphGrpc:Address is not configured.");

        builder.Services.AddGrpcClient<PartnerGraphApi.PartnerGraphApiClient>(options =>
        {
            options.Address = new Uri(partnerGraphGrpcAddress);
        });

        var app = builder.Build();

        await app.MigrateDatabaseAsync<CommissionsDbContext>();
        await app.InitializeCommissionSchemesAsync();
        await app.InitializeKafkaTopicsAsync(KafkaConstants.AllTopics);

        app.UseExceptionHandler();
        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((document, request) =>
            {
                if (request.Headers.TryGetValue("X-Gateway-Prefix", out var values))
                {
                    var prefix = values.ToString();
                    if (prefix.StartsWith('/')
                        && !prefix.StartsWith("//", StringComparison.Ordinal))
                    {
                        document.Servers = [new() { Url = prefix }];
                    }
                }
            });
        });
        app.MapControllers();

        await app.RunAsync();
    }
}

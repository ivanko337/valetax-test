namespace Wallet;

using Common.Application;
using Common.Constants;
using Common.Kafka;
using Common.Observability;
using Common.Outbox;
using Common.Persistence;
using Wallet.Application.Payouts;
using Wallet.Application.Wallets;
using Wallet.Infrastructure.Messaging;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Repositories;
using Wallet.Infrastructure.Processing;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceObservability();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new() { Title = "Wallet API", Version = "v1" }));
        builder.Services.AddPostgreSqlDbContext<WalletDbContext>(builder.Configuration);
        builder.Services.AddKafka(builder.Configuration);
        builder.Services.AddKafkaConsumer<UserRegisteredMessageHandler>(
            KafkaConstants.UserRegisteredTopic);
        builder.Services.AddKafkaConsumer<CommissionAccruedMessageHandler>(
            KafkaConstants.CommissionsAccruedTopic);
        builder.Services.AddOutbox<WalletDbContext, KafkaMessageTransport>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ICommissionPayoutProcessor, CommissionPayoutProcessor>();
        builder.Services.AddScoped<IWalletRepository, WalletRepository>();
        builder.Services.AddScoped<IWalletReader, WalletReader>();
        builder.Services.AddScoped<
            ICommandHandler<CreateWalletCommand, CreateWalletResult>,
            CreateWalletHandler>();
        builder.Services.AddScoped<
            IQueryHandler<GetWalletBalanceQuery, WalletBalance?>,
            GetWalletBalanceHandler>();
        builder.Services.AddScoped<
            IQueryHandler<GetPayoutHistoryQuery, IReadOnlyList<PayoutHistoryEntry>?>,
            GetPayoutHistoryHandler>();

        var app = builder.Build();

        await app.MigrateDatabaseAsync<WalletDbContext>();
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

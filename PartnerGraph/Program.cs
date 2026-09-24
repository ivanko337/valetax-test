namespace PartnerGraph;

using Common.Application;
using Common.Constants;
using Common.Kafka;
using Common.Observability;
using Common.Outbox;
using Common.Persistence;
using PartnerGraph.Application.Users;
using PartnerGraph.Infrastructure.Persistence;
using PartnerGraph.Infrastructure.Persistence.Repositories;
using PartnerGraph.Http;
using PartnerGraph.Services;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceObservability();

        builder.Services.AddControllers();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddSingleton<ApiProblemDetailsMapper>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new() { Title = "PartnerGraph API", Version = "v1" }));
        builder.Services.AddGrpc();
        builder.Services
            .AddOptions<PartnerGraphGrpcOptions>()
            .BindConfiguration(PartnerGraphGrpcOptions.SectionName)
            .Validate(
                options => options.MaxUplinePartners > 0,
                "PartnerGraphGrpc:MaxUplinePartners must be positive.")
            .ValidateOnStart();
        builder.Services.AddPostgreSqlDbContext<PartnerGraphDbContext>(builder.Configuration);
        builder.Services.AddKafka(builder.Configuration);
        builder.Services.AddOutbox<PartnerGraphDbContext, KafkaMessageTransport>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IPartnerGraphReader, PartnerGraphReader>();
        builder.Services.AddScoped<IPartnerLinkRepository, PartnerLinkRepository>();
        builder.Services.AddScoped<
            ICommandHandler<CreateUserCommand, CreateUserResult>,
            CreateUserHandler>();
        builder.Services.AddScoped<
            ICommandHandler<UpdateUserPartnerCommand, UpdateUserPartnerResult>,
            UpdateUserPartnerHandler>();
        builder.Services.AddScoped<
            IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?>,
            GetUplineHandler>();
        builder.Services.AddScoped<
            IQueryHandler<GetDownlineQuery, IReadOnlyList<Domain.Users.User>?>,
            GetDownlineHandler>();

        var app = builder.Build();

        await app.MigrateDatabaseAsync<PartnerGraphDbContext>();
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
        app.MapGrpcService<PartnerGraphGrpcService>();

        await app.RunAsync();
    }
}

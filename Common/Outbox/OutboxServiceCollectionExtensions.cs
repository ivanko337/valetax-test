using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Outbox;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        return services.AddOutbox<TDbContext, ConsoleMessageTransport>();
    }

    public static IServiceCollection AddOutbox<TDbContext, TTransport>(
        this IServiceCollection services)
        where TDbContext : DbContext
        where TTransport : class, IMessageTransport
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IOutboxWriter, EfOutboxWriter<TDbContext>>();
        services.AddScoped<OutboxBatchProcessor<TDbContext>>();
        services.AddSingleton<IMessageTransport, TTransport>();
        services.AddHostedService<OutboxWorker<TDbContext>>();

        return services;
    }
}

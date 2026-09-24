using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Common.Persistence;

public static class PostgreSqlServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSqlDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Database")
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' is not configured.");
        }

        services.AddDbContext<TDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(TDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure();
                }));

        return services;
    }

    public static async Task MigrateDatabaseAsync<TDbContext>(
        this IHost host,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);

        await using var scope = host.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(PostgreSqlServiceCollectionExtensions));
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        logger.LogInformation(
            "Applying database migrations for {DbContextName}",
            typeof(TDbContext).Name);

        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Database migrations for {DbContextName} are up to date",
            typeof(TDbContext).Name);
    }
}

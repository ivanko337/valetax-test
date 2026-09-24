using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Common.Persistence;

public static class PostgreSqlDesignTimeDbContextFactory
{
    public static DbContextOptions<TDbContext> CreateOptions<TDbContext>(
        string connectionStringName = "Database")
        where TDbContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' is not configured.");
        }

        return new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(
                connectionString,
                options => options.MigrationsAssembly(typeof(TDbContext).Assembly.FullName))
            .Options;
    }
}

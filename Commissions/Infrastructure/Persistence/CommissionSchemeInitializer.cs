using System.Data;
using Commissions.Domain.CommissionSchemes;
using Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace Commissions.Infrastructure.Persistence;

public static class CommissionSchemeInitializer
{
    public static async Task InitializeCommissionSchemesAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(CommissionSchemeInitializer));

        var result = await InitializeAsync(
            dbContext,
            timeProvider,
            cancellationToken);

        if (result.Created)
        {
            logger.LogInformation(
                "Created default commission scheme {SchemaType} version {Version}",
                result.SchemaType,
                result.Version);
        }
        else
        {
            logger.LogInformation(
                "Using existing commission scheme {SchemaType} version {Version}",
                result.SchemaType,
                result.Version);
        }
    }

    public static Task<CommissionSchemeInitializationResult> InitializeAsync(
        CommissionsDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var existing = await dbContext.CommissionSchemes
                .AsNoTracking()
                .OrderByDescending(scheme => scheme.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new CommissionSchemeInitializationResult(
                    existing.Version,
                    existing.SchemaType,
                    Created: false);
            }

            var scheme = new CommissionScheme(
                CommissionSchemaType.Linear,
                timeProvider.GetUtcNow());
            dbContext.CommissionSchemes.Add(scheme);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new CommissionSchemeInitializationResult(
                scheme.Version,
                scheme.SchemaType,
                Created: true);
        });
    }
}

public sealed record CommissionSchemeInitializationResult(
    int Version,
    CommissionSchemaType SchemaType,
    bool Created);

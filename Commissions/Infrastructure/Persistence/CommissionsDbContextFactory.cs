using Common.Persistence;
using Microsoft.EntityFrameworkCore.Design;

namespace Commissions.Infrastructure.Persistence;

public sealed class CommissionsDbContextFactory
    : IDesignTimeDbContextFactory<CommissionsDbContext>
{
    public CommissionsDbContext CreateDbContext(string[] args)
    {
        return new CommissionsDbContext(
            PostgreSqlDesignTimeDbContextFactory.CreateOptions<CommissionsDbContext>());
    }
}

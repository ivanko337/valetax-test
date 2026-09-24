using Common.Persistence;
using Microsoft.EntityFrameworkCore.Design;

namespace PartnerGraph.Infrastructure.Persistence;

public sealed class PartnerGraphDbContextFactory
    : IDesignTimeDbContextFactory<PartnerGraphDbContext>
{
    public PartnerGraphDbContext CreateDbContext(string[] args)
    {
        return new PartnerGraphDbContext(
            PostgreSqlDesignTimeDbContextFactory.CreateOptions<PartnerGraphDbContext>());
    }
}

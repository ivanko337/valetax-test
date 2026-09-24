using Commissions.Domain.Commissions;
using Commissions.Domain.CommissionSchemes;
using Commissions.Domain.ProfitEvents;
using Common.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Commissions.Infrastructure.Persistence;

public sealed class CommissionsDbContext(DbContextOptions<CommissionsDbContext> options)
    : DbContext(options)
{
    public DbSet<ProfitEvent> ProfitEvents => Set<ProfitEvent>();

    public DbSet<CommissionScheme> CommissionSchemes => Set<CommissionScheme>();

    public DbSet<Commission> Commissions => Set<Commission>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommissionsDbContext).Assembly);
        modelBuilder.AddOutbox();
    }
}

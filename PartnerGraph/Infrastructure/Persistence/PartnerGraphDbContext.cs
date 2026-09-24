using Common.Outbox;
using Microsoft.EntityFrameworkCore;
using PartnerGraph.Domain.Users;

namespace PartnerGraph.Infrastructure.Persistence;

public sealed class PartnerGraphDbContext(DbContextOptions<PartnerGraphDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartnerGraphDbContext).Assembly);
        modelBuilder.AddOutbox();
    }
}

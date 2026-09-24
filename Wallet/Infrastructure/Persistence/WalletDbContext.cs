using Common.Outbox;
using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Payouts;

namespace Wallet.Infrastructure.Persistence;

public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options)
    : DbContext(options)
{
    public DbSet<Domain.Wallets.Wallet> Wallets => Set<Domain.Wallets.Wallet>();

    public DbSet<Payout> Payouts => Set<Payout>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
        modelBuilder.AddOutbox();
    }
}

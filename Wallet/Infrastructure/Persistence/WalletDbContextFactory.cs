using Common.Persistence;
using Microsoft.EntityFrameworkCore.Design;

namespace Wallet.Infrastructure.Persistence;

public sealed class WalletDbContextFactory
    : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {
        return new WalletDbContext(
            PostgreSqlDesignTimeDbContextFactory.CreateOptions<WalletDbContext>());
    }
}

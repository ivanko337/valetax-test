using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet.Application.Wallets;

namespace Wallet.Infrastructure.Persistence.Repositories;

public sealed class WalletRepository(WalletDbContext dbContext) : IWalletRepository
{
    private const string WalletPrimaryKeyConstraint = "PK_Wallets";

    public async Task<bool> TryAddAsync(
        Domain.Wallets.Wallet wallet,
        CancellationToken cancellationToken)
    {
        dbContext.Wallets.Add(wallet);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (IsWalletPrimaryKeyViolation(exception))
        {
            dbContext.Entry(wallet).State = EntityState.Detached;
            return false;
        }
    }

    private static bool IsWalletPrimaryKeyViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName == WalletPrimaryKeyConstraint;
    }
}

using Microsoft.EntityFrameworkCore;
using Npgsql;
using PartnerGraph.Application.Users;
using PartnerGraph.Domain.Users;

namespace PartnerGraph.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(PartnerGraphDbContext dbContext) : IUserRepository
{
    private const string PrimaryKeyConstraint = "PK_Users";
    private const string PartnerForeignKeyConstraint = "FK_Users_Users_PartnerId";

    public Task<User?> FindByExternalIdAsync(
        Guid externalId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.ExternalId == externalId, cancellationToken);
    }

    public async Task<AddUserResult> TryAddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return AddUserResult.Added;
        }
        catch (DbUpdateException exception)
            when (IsConstraintViolation(exception, PostgresErrorCodes.UniqueViolation, PrimaryKeyConstraint))
        {
            dbContext.ChangeTracker.Clear();
            return AddUserResult.ExternalIdAlreadyExists;
        }
        catch (DbUpdateException exception)
            when (IsConstraintViolation(exception, PostgresErrorCodes.ForeignKeyViolation, PartnerForeignKeyConstraint))
        {
            dbContext.ChangeTracker.Clear();
            return AddUserResult.PartnerDoesNotExist;
        }
    }

    private static bool IsConstraintViolation(
        DbUpdateException exception,
        string sqlState,
        string constraintName)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == sqlState
            && postgresException.ConstraintName == constraintName;
    }
}

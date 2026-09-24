using System.Data;
using Microsoft.EntityFrameworkCore;
using PartnerGraph.Application.Users;

namespace PartnerGraph.Infrastructure.Persistence.Repositories;

public sealed class PartnerLinkRepository(
    PartnerGraphDbContext dbContext,
    IPartnerGraphReader partnerGraphReader)
    : IPartnerLinkRepository
{
    public Task<UpdateUserPartnerResult> UpdateAsync(
        Guid externalId,
        Guid? partnerId,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            // A serialization retry reuses this scoped DbContext. Do not retain
            // entities or state from the failed attempt.
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var user = await dbContext.Users.SingleOrDefaultAsync(
                item => item.ExternalId == externalId,
                cancellationToken);

            if (user is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new UpdateUserPartnerResult(UpdateUserPartnerStatus.UserDoesNotExist);
            }

            if (user.PartnerId == partnerId)
            {
                await transaction.CommitAsync(cancellationToken);
                return new UpdateUserPartnerResult(
                    UpdateUserPartnerStatus.Unchanged,
                    user);
            }

            if (partnerId is { } newPartnerId)
            {
                if (newPartnerId == externalId)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new UpdateUserPartnerResult(UpdateUserPartnerStatus.CycleDetected);
                }

                var upline = await partnerGraphReader.GetUplineAsync(
                    newPartnerId,
                    cancellationToken);

                if (!upline.UserExists)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new UpdateUserPartnerResult(UpdateUserPartnerStatus.PartnerDoesNotExist);
                }

                if (upline.Partners.Any(item => item.User.ExternalId == externalId))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new UpdateUserPartnerResult(UpdateUserPartnerStatus.CycleDetected);
                }
            }

            user.AssignPartner(partnerId);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new UpdateUserPartnerResult(UpdateUserPartnerStatus.Updated, user);
        });
    }
}

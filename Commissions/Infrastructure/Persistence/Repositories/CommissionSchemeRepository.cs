using Commissions.Application.CommissionSchemes;
using Commissions.Domain.CommissionSchemes;

namespace Commissions.Infrastructure.Persistence.Repositories;

public sealed class CommissionSchemeRepository(CommissionsDbContext dbContext)
    : ICommissionSchemeRepository
{
    public async Task AddAsync(
        CommissionScheme commissionScheme,
        CancellationToken cancellationToken)
    {
        dbContext.CommissionSchemes.Add(commissionScheme);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

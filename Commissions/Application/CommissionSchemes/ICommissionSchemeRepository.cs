using Commissions.Domain.CommissionSchemes;

namespace Commissions.Application.CommissionSchemes;

public interface ICommissionSchemeRepository
{
    Task AddAsync(
        CommissionScheme commissionScheme,
        CancellationToken cancellationToken);
}

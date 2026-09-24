using PartnerGraph.Domain.Users;

namespace PartnerGraph.Application.Users;

public interface IUserRepository
{
    Task<User?> FindByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid externalId, CancellationToken cancellationToken);

    Task<AddUserResult> TryAddAsync(User user, CancellationToken cancellationToken);
}

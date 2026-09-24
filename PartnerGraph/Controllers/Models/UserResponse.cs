namespace PartnerGraph.Controllers.Models;

public sealed record UserResponse(
    Guid ExternalId,
    Guid? PartnerId,
    DateTimeOffset CreatedAt)
{
    internal static UserResponse From(Domain.Users.User user)
    {
        return new UserResponse(user.ExternalId, user.PartnerId, user.CreatedAt);
    }
}

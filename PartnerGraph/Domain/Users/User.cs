namespace PartnerGraph.Domain.Users;

public sealed class User(
    Guid externalId,
    Guid? partnerId,
    DateTimeOffset createdAt)
{
    public Guid ExternalId { get; private set; } = externalId;

    public Guid? PartnerId { get; private set; } = partnerId;

    public DateTimeOffset CreatedAt { get; private set; } = createdAt;

    public void AssignPartner(Guid? partnerId)
    {
        PartnerId = partnerId;
    }
}

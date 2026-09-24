namespace PartnerGraph.Application.Users;

public enum UpdateUserPartnerStatus
{
    Updated,
    Unchanged,
    InvalidExternalId,
    InvalidPartnerId,
    UserDoesNotExist,
    PartnerDoesNotExist,
    CycleDetected
}

namespace PartnerGraph.Application.Users;

public enum CreateUserStatus
{
    Created,
    AlreadyExists,
    InvalidExternalId,
    InvalidPartnerId,
    PartnerDoesNotExist,
    CycleDetected,
    ExternalIdConflict
}

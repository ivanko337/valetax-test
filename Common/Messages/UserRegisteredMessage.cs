namespace Common.Messages;

public sealed record UserRegisteredMessage(
    Guid ExternalId,
    DateTimeOffset CreatedAt);

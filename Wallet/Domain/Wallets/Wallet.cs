namespace Wallet.Domain.Wallets;

public sealed class Wallet(
    Guid userExternalId,
    long balanceCents,
    DateTimeOffset createdAt)
{
    public Guid UserExternalId { get; private set; } = userExternalId;

    public long BalanceCents { get; private set; } = balanceCents;

    public DateTimeOffset CreatedAt { get; private set; } = createdAt;
}

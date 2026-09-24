using Common.Enums;

namespace Commissions.Domain.CommissionSchemes;

public sealed class CommissionScheme(
    CommissionSchemaType schemaType,
    DateTimeOffset changedAt)
{
    public int Version { get; private set; }

    public CommissionSchemaType SchemaType { get; private set; } = schemaType;

    public DateTimeOffset ChangedAt { get; private set; } = changedAt;
}

using Common.Enums;

namespace Commissions.Application.CommissionSchemes;

public sealed record CommissionSchemeResult(
    int Version,
    CommissionSchemaType SchemaType,
    DateTimeOffset ChangedAt);

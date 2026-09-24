using Commissions.Application.CommissionSchemes;
using Common.Enums;

namespace Commissions.Controllers.Models;

public sealed record CommissionSchemeResponse(
    int Version,
    CommissionSchemaType SchemaType,
    DateTimeOffset ChangedAt)
{
    public static CommissionSchemeResponse From(CommissionSchemeResult scheme)
    {
        return new CommissionSchemeResponse(
            scheme.Version,
            scheme.SchemaType,
            scheme.ChangedAt);
    }
}

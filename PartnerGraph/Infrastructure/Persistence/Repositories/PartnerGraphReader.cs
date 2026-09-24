using Microsoft.EntityFrameworkCore;
using PartnerGraph.Application.Users;
using PartnerGraph.Domain.Users;

namespace PartnerGraph.Infrastructure.Persistence.Repositories;

public sealed class PartnerGraphReader(PartnerGraphDbContext dbContext) : IPartnerGraphReader
{
    public Task<bool> PartnerChainContainsAsync(
        Guid partnerId,
        Guid externalId,
        CancellationToken cancellationToken)
    {
        return dbContext.Database
            .SqlQuery<bool>($"""
                WITH RECURSIVE upline AS
                (
                    SELECT "ExternalId", "PartnerId"
                    FROM "Users"
                    WHERE "ExternalId" = {partnerId}

                    UNION

                    SELECT parent."ExternalId", parent."PartnerId"
                    FROM "Users" AS parent
                    INNER JOIN upline AS child
                        ON parent."ExternalId" = child."PartnerId"
                )
                SELECT EXISTS
                (
                    SELECT 1
                    FROM upline
                    WHERE "ExternalId" = {externalId}
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
    }

    public async Task<PartnerTraversalResult<PartnerAtLevel>> GetUplineAsync(
        Guid externalId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database
            .SqlQuery<PartnerGraphRow>($"""
                WITH RECURSIVE upline AS
                (
                    SELECT
                        "ExternalId",
                        "PartnerId",
                        "CreatedAt",
                        0 AS "Level",
                        ARRAY["ExternalId"] AS path,
                        FALSE AS "CycleDetected"
                    FROM "Users"
                    WHERE "ExternalId" = {externalId}

                    UNION ALL

                    SELECT
                        partner."ExternalId",
                        partner."PartnerId",
                        partner."CreatedAt",
                        child."Level" + 1 AS "Level",
                        child.path || partner."ExternalId" AS path,
                        partner."ExternalId" = ANY(child.path) AS "CycleDetected"
                    FROM upline AS child
                    INNER JOIN "Users" AS partner
                        ON partner."ExternalId" = child."PartnerId"
                    WHERE NOT child."CycleDetected"
                )
                SELECT
                    "ExternalId",
                    "PartnerId",
                    "CreatedAt",
                    "Level",
                    "CycleDetected"
                FROM upline
                """)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new PartnerTraversalResult<PartnerAtLevel>(false, []);
        }

        ThrowIfCycleDetected(rows, externalId, "upline");

        var partners = rows
            .Where(row => row.Level > 0)
            .Select(row => new PartnerAtLevel(row.ToUser(), row.Level))
            .ToArray();

        return new PartnerTraversalResult<PartnerAtLevel>(true, partners);
    }

    public async Task<PartnerTraversalResult<User>> GetDownlineAsync(
        Guid externalId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database
            .SqlQuery<PartnerGraphRow>($"""
                WITH RECURSIVE downline AS
                (
                    SELECT
                        "ExternalId",
                        "PartnerId",
                        "CreatedAt",
                        0 AS "Level",
                        ARRAY["ExternalId"] AS path,
                        FALSE AS "CycleDetected"
                    FROM "Users"
                    WHERE "ExternalId" = {externalId}

                    UNION ALL

                    SELECT
                        child."ExternalId",
                        child."PartnerId",
                        child."CreatedAt",
                        parent."Level" + 1 AS "Level",
                        parent.path || child."ExternalId" AS path,
                        child."ExternalId" = ANY(parent.path) AS "CycleDetected"
                    FROM downline AS parent
                    INNER JOIN "Users" AS child
                        ON child."PartnerId" = parent."ExternalId"
                    WHERE NOT parent."CycleDetected"
                )
                SELECT
                    "ExternalId",
                    "PartnerId",
                    "CreatedAt",
                    "Level",
                    "CycleDetected"
                FROM downline
                """)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new PartnerTraversalResult<User>(false, []);
        }

        ThrowIfCycleDetected(rows, externalId, "downline");

        var partners = rows
            .Where(row => row.Level > 0)
            .Select(row => row.ToUser())
            .ToArray();

        return new PartnerTraversalResult<User>(true, partners);
    }

    private static void ThrowIfCycleDetected(
        IEnumerable<PartnerGraphRow> rows,
        Guid externalId,
        string direction)
    {
        if (rows.Any(x => x.CycleDetected))
        {
            throw new PartnerGraphCycleException(externalId, direction);
        }
    }

    private sealed record PartnerGraphRow(
        Guid ExternalId,
        Guid? PartnerId,
        DateTimeOffset CreatedAt,
        int Level,
        bool CycleDetected)
    {
        public User ToUser() => new(ExternalId, PartnerId, CreatedAt);
    }
}

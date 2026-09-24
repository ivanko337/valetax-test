using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerGraph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreventSelfPartner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PartnerId_NotSelf",
                table: "Users",
                sql: "\"PartnerId\" IS NULL OR \"PartnerId\" <> \"ExternalId\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PartnerId_NotSelf",
                table: "Users");
        }
    }
}

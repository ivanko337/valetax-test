using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Commissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitEventUserLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProfitEvents_UserExternalId_OcurredAt",
                table: "ProfitEvents",
                columns: new[] { "UserExternalId", "OcurredAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfitEvents_UserExternalId_OcurredAt",
                table: "ProfitEvents");
        }
    }
}

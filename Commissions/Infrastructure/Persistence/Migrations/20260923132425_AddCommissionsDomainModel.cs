using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Commissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionsDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommissionsSchemes",
                columns: table => new
                {
                    Version = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SchemaType = table.Column<byte>(type: "smallint", nullable: false, comment: "Possible values of enum type: CommissionSchemaType\n----------\n0 -- Linear\n1 -- Fibonacci\n"),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionsSchemes", x => x.Version);
                });

            migrationBuilder.CreateTable(
                name: "ProfitEvents",
                columns: table => new
                {
                    ExternalEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserExternalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitCents = table.Column<long>(type: "bigint", nullable: false),
                    OcurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false, comment: "Possible values of enum type: ProfitEventStatus\n----------\n0 -- PendingCalculation\n1 -- Calculated\n2 -- Failed\n"),
                    SchemaType = table.Column<byte>(type: "smallint", nullable: false, comment: "Possible values of enum type: CommissionSchemaType\n----------\n0 -- Linear\n1 -- Fibonacci\n"),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitEvents", x => x.ExternalEventId);
                });

            migrationBuilder.CreateTable(
                name: "Commissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<byte>(type: "smallint", nullable: false, comment: "Possible values of enum type: CommissionPaymentStatus\n----------\n0 -- Pending\n1 -- Paid\n"),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commissions_CommissionsSchemes_SchemaVersion",
                        column: x => x.SchemaVersion,
                        principalTable: "CommissionsSchemes",
                        principalColumn: "Version",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Commissions_ProfitEvents_ExternalEventId",
                        column: x => x.ExternalEventId,
                        principalTable: "ProfitEvents",
                        principalColumn: "ExternalEventId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_ExternalEventId",
                table: "Commissions",
                column: "ExternalEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_SchemaVersion",
                table: "Commissions",
                column: "SchemaVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Commissions");

            migrationBuilder.DropTable(
                name: "CommissionsSchemes");

            migrationBuilder.DropTable(
                name: "ProfitEvents");
        }
    }
}

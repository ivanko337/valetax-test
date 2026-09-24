using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Commissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitEventProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commissions_ExternalEventId",
                table: "Commissions");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "ProfitEvents",
                type: "smallint",
                nullable: false,
                comment: "Possible values of enum type: ProfitEventStatus\n----------\n0 -- PendingCalculation\n1 -- Calculated\n2 -- Failed\n3 -- Calculating\n",
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldComment: "Possible values of enum type: ProfitEventStatus\n----------\n0 -- PendingCalculation\n1 -- Calculated\n2 -- Failed\n");

            migrationBuilder.AlterColumn<byte>(
                name: "SchemaType",
                table: "ProfitEvents",
                type: "smallint",
                nullable: true,
                comment: "Possible values of enum type: CommissionSchemaType\n----------\n0 -- Linear\n1 -- Fibonacci\n",
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldComment: "Possible values of enum type: CommissionSchemaType\n----------\n0 -- Linear\n1 -- Fibonacci\n");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CalculatedAt",
                table: "ProfitEvents",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "CalculationAttempts",
                table: "ProfitEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CalculationStartedAt",
                table: "ProfitEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CalculationToken",
                table: "ProfitEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextCalculationAttemptAt",
                table: "ProfitEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitEvents_PendingCalculation",
                table: "ProfitEvents",
                columns: new[] { "Status", "NextCalculationAttemptAt", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitEvents_StaleCalculation",
                table: "ProfitEvents",
                columns: new[] { "Status", "CalculationStartedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProfitEvents_CalculationAttempts",
                table: "ProfitEvents",
                sql: "\"CalculationAttempts\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_ExternalEventId_BeneficiaryId",
                table: "Commissions",
                columns: new[] { "ExternalEventId", "BeneficiaryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfitEvents_PendingCalculation",
                table: "ProfitEvents");

            migrationBuilder.DropIndex(
                name: "IX_ProfitEvents_StaleCalculation",
                table: "ProfitEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProfitEvents_CalculationAttempts",
                table: "ProfitEvents");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_ExternalEventId_BeneficiaryId",
                table: "Commissions");

            migrationBuilder.DropColumn(
                name: "CalculationAttempts",
                table: "ProfitEvents");

            migrationBuilder.DropColumn(
                name: "CalculationStartedAt",
                table: "ProfitEvents");

            migrationBuilder.DropColumn(
                name: "CalculationToken",
                table: "ProfitEvents");

            migrationBuilder.DropColumn(
                name: "NextCalculationAttemptAt",
                table: "ProfitEvents");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "ProfitEvents",
                type: "smallint",
                nullable: false,
                comment: "Possible values of enum type: ProfitEventStatus\n----------\n0 -- PendingCalculation\n1 -- Calculated\n2 -- Failed\n",
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldComment: "Possible values of enum type: ProfitEventStatus\n----------\n0 -- PendingCalculation\n1 -- Calculated\n2 -- Failed\n3 -- Calculating\n");

            migrationBuilder.AlterColumn<byte>(
                name: "SchemaType",
                table: "ProfitEvents",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "Possible values of enum type: CommissionSchemaType\n----------\n0 -- Linear\n1 -- Fibonacci\n",
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldNullable: true,
                oldComment: "Possible values of enum type: CommissionSchemaType\n----------\n0 -- Linear\n1 -- Fibonacci\n");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CalculatedAt",
                table: "ProfitEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_ExternalEventId",
                table: "Commissions",
                column: "ExternalEventId");
        }
    }
}

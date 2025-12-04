using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentContributionEndConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EndAmountThreshold",
                table: "InvestmentContributions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EndConditionAccountId",
                table: "InvestmentContributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndConditionType",
                table: "InvestmentContributions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "InvestmentContributions",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentContributions_EndConditionAccountId",
                table: "InvestmentContributions",
                column: "EndConditionAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestmentContributions_accounts_EndConditionAccountId",
                table: "InvestmentContributions",
                column: "EndConditionAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentContributions_accounts_EndConditionAccountId",
                table: "InvestmentContributions");

            migrationBuilder.DropIndex(
                name: "IX_InvestmentContributions_EndConditionAccountId",
                table: "InvestmentContributions");

            migrationBuilder.DropColumn(
                name: "EndAmountThreshold",
                table: "InvestmentContributions");

            migrationBuilder.DropColumn(
                name: "EndConditionAccountId",
                table: "InvestmentContributions");

            migrationBuilder.DropColumn(
                name: "EndConditionType",
                table: "InvestmentContributions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "InvestmentContributions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEndConditionsToIncomeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EndAmountThreshold",
                table: "income_sources",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EndConditionAccountId",
                table: "income_sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndConditionType",
                table: "income_sources",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "income_sources",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_income_sources_EndConditionAccountId",
                table: "income_sources",
                column: "EndConditionAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_income_sources_accounts_EndConditionAccountId",
                table: "income_sources",
                column: "EndConditionAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_income_sources_accounts_EndConditionAccountId",
                table: "income_sources");

            migrationBuilder.DropIndex(
                name: "IX_income_sources_EndConditionAccountId",
                table: "income_sources");

            migrationBuilder.DropColumn(
                name: "EndAmountThreshold",
                table: "income_sources");

            migrationBuilder.DropColumn(
                name: "EndConditionAccountId",
                table: "income_sources");

            migrationBuilder.DropColumn(
                name: "EndConditionType",
                table: "income_sources");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "income_sources");
        }
    }
}

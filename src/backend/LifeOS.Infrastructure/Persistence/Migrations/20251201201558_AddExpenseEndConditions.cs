using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseEndConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EndAmountThreshold",
                table: "expense_definitions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EndConditionAccountId",
                table: "expense_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndConditionType",
                table: "expense_definitions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "expense_definitions",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_expense_definitions_EndConditionAccountId",
                table: "expense_definitions",
                column: "EndConditionAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_expense_definitions_accounts_EndConditionAccountId",
                table: "expense_definitions",
                column: "EndConditionAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expense_definitions_accounts_EndConditionAccountId",
                table: "expense_definitions");

            migrationBuilder.DropIndex(
                name: "IX_expense_definitions_EndConditionAccountId",
                table: "expense_definitions");

            migrationBuilder.DropColumn(
                name: "EndAmountThreshold",
                table: "expense_definitions");

            migrationBuilder.DropColumn(
                name: "EndConditionAccountId",
                table: "expense_definitions");

            migrationBuilder.DropColumn(
                name: "EndConditionType",
                table: "expense_definitions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "expense_definitions");
        }
    }
}

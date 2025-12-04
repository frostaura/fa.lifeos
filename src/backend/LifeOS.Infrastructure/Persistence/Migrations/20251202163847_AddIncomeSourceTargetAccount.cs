using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeSourceTargetAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetAccountId",
                table: "income_sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_income_sources_TargetAccountId",
                table: "income_sources",
                column: "TargetAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_income_sources_accounts_TargetAccountId",
                table: "income_sources",
                column: "TargetAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_income_sources_accounts_TargetAccountId",
                table: "income_sources");

            migrationBuilder.DropIndex(
                name: "IX_income_sources_TargetAccountId",
                table: "income_sources");

            migrationBuilder.DropColumn(
                name: "TargetAccountId",
                table: "income_sources");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V3_0_LongevityRiskFactorModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_longevity_models_InputMetrics",
                table: "longevity_models");

            migrationBuilder.DropColumn(
                name: "OutputUnit",
                table: "longevity_models");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "longevity_models");

            migrationBuilder.AlterColumn<string>(
                name: "ModelType",
                table: "longevity_models",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "linear");

            // Change InputMetrics from text[] to jsonb with explicit USING clause
            migrationBuilder.Sql(@"
                ALTER TABLE longevity_models 
                ALTER COLUMN ""InputMetrics"" TYPE jsonb 
                USING to_jsonb(""InputMetrics"");
            ");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxRiskReduction",
                table: "longevity_models",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "longevity_models",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_longevity_models_UserId",
                table: "longevity_models",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_longevity_models_UserId",
                table: "longevity_models");

            migrationBuilder.DropColumn(
                name: "MaxRiskReduction",
                table: "longevity_models");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "longevity_models");

            migrationBuilder.AlterColumn<string>(
                name: "ModelType",
                table: "longevity_models",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "linear",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string[]>(
                name: "InputMetrics",
                table: "longevity_models",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<string>(
                name: "OutputUnit",
                table: "longevity_models",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "years_added");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "longevity_models",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_longevity_models_InputMetrics",
                table: "longevity_models",
                column: "InputMetrics")
                .Annotation("Npgsql:IndexMethod", "gin");
        }
    }
}

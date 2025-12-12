using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V3_1_LongevityCalculationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustedLifeExpectancy",
                table: "longevity_snapshots");

            migrationBuilder.DropColumn(
                name: "ConfidenceLevel",
                table: "longevity_snapshots");

            migrationBuilder.DropColumn(
                name: "InputMetricsSnapshot",
                table: "longevity_snapshots");

            migrationBuilder.RenameColumn(
                name: "EstimatedYearsAdded",
                table: "longevity_snapshots",
                newName: "BaselineLifeExpectancyYears");

            migrationBuilder.RenameColumn(
                name: "CalculatedAt",
                table: "longevity_snapshots",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "BaselineLifeExpectancy",
                table: "longevity_snapshots",
                newName: "AdjustedLifeExpectancyYears");

            migrationBuilder.RenameIndex(
                name: "IX_longevity_snapshots_UserId_CalculatedAt",
                table: "longevity_snapshots",
                newName: "IX_longevity_snapshots_UserId_Timestamp");

            migrationBuilder.AddColumn<string>(
                name: "Confidence",
                table: "longevity_snapshots",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "medium");

            migrationBuilder.AddColumn<decimal>(
                name: "RiskFactorCombined",
                table: "longevity_snapshots",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalYearsAdded",
                table: "longevity_snapshots",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "longevity_snapshots");

            migrationBuilder.DropColumn(
                name: "RiskFactorCombined",
                table: "longevity_snapshots");

            migrationBuilder.DropColumn(
                name: "TotalYearsAdded",
                table: "longevity_snapshots");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "longevity_snapshots",
                newName: "CalculatedAt");

            migrationBuilder.RenameColumn(
                name: "BaselineLifeExpectancyYears",
                table: "longevity_snapshots",
                newName: "EstimatedYearsAdded");

            migrationBuilder.RenameColumn(
                name: "AdjustedLifeExpectancyYears",
                table: "longevity_snapshots",
                newName: "BaselineLifeExpectancy");

            migrationBuilder.RenameIndex(
                name: "IX_longevity_snapshots_UserId_Timestamp",
                table: "longevity_snapshots",
                newName: "IX_longevity_snapshots_UserId_CalculatedAt");

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedLifeExpectancy",
                table: "longevity_snapshots",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ConfidenceLevel",
                table: "longevity_snapshots",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "moderate");

            migrationBuilder.AddColumn<string>(
                name: "InputMetricsSnapshot",
                table: "longevity_snapshots",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}

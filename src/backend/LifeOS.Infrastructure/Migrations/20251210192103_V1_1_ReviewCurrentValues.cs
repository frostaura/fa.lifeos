using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_1_ReviewCurrentValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdherenceIndexCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HealthIndexCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LongevityCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryStatsCurrent",
                table: "review_snapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WealthHealthCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdherenceIndexCurrent",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "HealthIndexCurrent",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "LongevityCurrent",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "PrimaryStatsCurrent",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "WealthHealthCurrent",
                table: "review_snapshots");
        }
    }
}

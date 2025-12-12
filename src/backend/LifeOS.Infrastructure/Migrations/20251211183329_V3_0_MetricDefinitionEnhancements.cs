using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V3_0_MetricDefinitionEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add temporary column for string values
            migrationBuilder.AddColumn<string>(
                name: "TargetDirection_temp",
                table: "metric_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Step 2: Convert existing integer values to string enum names
            // 0 = AtOrAbove, 1 = AtOrBelow, 2 = Range
            migrationBuilder.Sql(@"
                UPDATE metric_definitions 
                SET ""TargetDirection_temp"" = CASE ""TargetDirection""
                    WHEN 0 THEN 'AtOrAbove'
                    WHEN 1 THEN 'AtOrBelow'
                    WHEN 2 THEN 'Range'
                    ELSE 'AtOrAbove'
                END;
            ");

            // Step 3: Drop old integer column
            migrationBuilder.DropColumn(
                name: "TargetDirection",
                table: "metric_definitions");

            // Step 4: Rename temporary column to TargetDirection
            migrationBuilder.RenameColumn(
                name: "TargetDirection_temp",
                table: "metric_definitions",
                newName: "TargetDirection");

            // Step 5: Make the column non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "TargetDirection",
                table: "metric_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "AtOrAbove");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: Convert string back to integer
            migrationBuilder.AddColumn<int>(
                name: "TargetDirection_temp",
                table: "metric_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE metric_definitions 
                SET ""TargetDirection_temp"" = CASE ""TargetDirection""
                    WHEN 'AtOrAbove' THEN 0
                    WHEN 'AtOrBelow' THEN 1
                    WHEN 'Range' THEN 2
                    ELSE 0
                END;
            ");

            migrationBuilder.DropColumn(
                name: "TargetDirection",
                table: "metric_definitions");

            migrationBuilder.RenameColumn(
                name: "TargetDirection_temp",
                table: "metric_definitions",
                newName: "TargetDirection");

            migrationBuilder.AlterColumn<int>(
                name: "TargetDirection",
                table: "metric_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

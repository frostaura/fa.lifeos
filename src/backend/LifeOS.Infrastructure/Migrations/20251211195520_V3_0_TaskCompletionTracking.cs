using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V3_0_TaskCompletionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old indexes if they exist (conditional for fresh databases)
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_task_completions_task_id_completed_at"";
                DROP INDEX IF EXISTS ""IX_task_completions_user_id"";
            ");

            migrationBuilder.AlterColumn<decimal>(
                name: "value_number",
                table: "task_completions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,4)",
                oldPrecision: 19,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "completion_type",
                table: "task_completions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateIndex(
                name: "idx_task_completions_task_id",
                table: "task_completions",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "idx_task_completions_user_id_completed_at",
                table: "task_completions",
                columns: new[] { "user_id", "completed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_task_completions_task_id",
                table: "task_completions");

            migrationBuilder.DropIndex(
                name: "idx_task_completions_user_id_completed_at",
                table: "task_completions");

            migrationBuilder.DropColumn(
                name: "completion_type",
                table: "task_completions");

            migrationBuilder.AlterColumn<decimal>(
                name: "value_number",
                table: "task_completions",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_completions_task_id_completed_at",
                table: "task_completions",
                columns: new[] { "task_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_task_completions_user_id",
                table: "task_completions",
                column: "user_id");
        }
    }
}

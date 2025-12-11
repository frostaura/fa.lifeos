using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_2_CoreEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WealthHealthCurrent",
                table: "review_snapshots",
                newName: "wealth_health_current");

            migrationBuilder.RenameColumn(
                name: "PrimaryStatsCurrent",
                table: "review_snapshots",
                newName: "primary_stats_current");

            migrationBuilder.RenameColumn(
                name: "LongevityCurrent",
                table: "review_snapshots",
                newName: "longevity_current");

            migrationBuilder.RenameColumn(
                name: "HealthIndexCurrent",
                table: "review_snapshots",
                newName: "health_index_current");

            migrationBuilder.RenameColumn(
                name: "AdherenceIndexCurrent",
                table: "review_snapshots",
                newName: "adherence_index_current");

            migrationBuilder.RenameColumn(
                name: "linked_milestone_ids",
                table: "identity_profiles",
                newName: "linked_milestones");

            migrationBuilder.AlterColumn<decimal>(
                name: "wealth_health_current",
                table: "review_snapshots",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "primary_stats_current",
                table: "review_snapshots",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "longevity_current",
                table: "review_snapshots",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "health_index_current",
                table: "review_snapshots",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "adherence_index_current",
                table: "review_snapshots",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dimension_scores",
                table: "review_snapshots",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "net_cash_flow",
                table: "review_snapshots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "net_worth_current",
                table: "review_snapshots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "net_worth_delta",
                table: "review_snapshots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "savings_rate",
                table: "review_snapshots",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_expenses",
                table: "review_snapshots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_income",
                table: "review_snapshots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "adherence_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    time_window_days = table.Column<int>(type: "integer", nullable: false),
                    tasks_considered = table.Column<int>(type: "integer", nullable: false),
                    tasks_completed = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adherence_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_adherence_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_index_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    components = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_index_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_health_index_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lifeos_score_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    life_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    health_index = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    adherence_index = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    wealth_health_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    longevity_years_added = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    dimension_scores = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lifeos_score_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_lifeos_score_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "primary_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_primary_stats", x => x.id);
                    table.UniqueConstraint("AK_primary_stats_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "simulation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    summary = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_simulation_runs_simulation_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "simulation_scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_simulation_runs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_completions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    value_number = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_completions", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_completions_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_completions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    baseline_life_expectancy_years = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    default_inflation_rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    default_investment_growth_rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    streak_penalty_sensitivity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wealth_health_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    components = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wealth_health_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_wealth_health_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dimension_primary_stat_weights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dimension_id = table.Column<Guid>(type: "uuid", nullable: false),
                    primary_stat_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dimension_primary_stat_weights", x => x.id);
                    table.ForeignKey(
                        name: "FK_dimension_primary_stat_weights_dimensions_dimension_id",
                        column: x => x.dimension_id,
                        principalTable: "dimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dimension_primary_stat_weights_primary_stats_primary_stat_c~",
                        column: x => x.primary_stat_code,
                        principalTable: "primary_stats",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adherence_snapshots_user_id_timestamp",
                table: "adherence_snapshots",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_dimension_primary_stat_weights_dimension_id_primary_stat_co~",
                table: "dimension_primary_stat_weights",
                columns: new[] { "dimension_id", "primary_stat_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dimension_primary_stat_weights_primary_stat_code",
                table: "dimension_primary_stat_weights",
                column: "primary_stat_code");

            migrationBuilder.CreateIndex(
                name: "IX_health_index_snapshots_user_id_timestamp",
                table: "health_index_snapshots",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_lifeos_score_snapshots_user_id_timestamp",
                table: "lifeos_score_snapshots",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_primary_stats_code",
                table: "primary_stats",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_simulation_runs_scenario_id_started_at",
                table: "simulation_runs",
                columns: new[] { "scenario_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_simulation_runs_user_id",
                table: "simulation_runs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_completions_task_id_completed_at",
                table: "task_completions",
                columns: new[] { "task_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_task_completions_user_id",
                table: "task_completions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wealth_health_snapshots_user_id_timestamp",
                table: "wealth_health_snapshots",
                columns: new[] { "user_id", "timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adherence_snapshots");

            migrationBuilder.DropTable(
                name: "dimension_primary_stat_weights");

            migrationBuilder.DropTable(
                name: "health_index_snapshots");

            migrationBuilder.DropTable(
                name: "lifeos_score_snapshots");

            migrationBuilder.DropTable(
                name: "simulation_runs");

            migrationBuilder.DropTable(
                name: "task_completions");

            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropTable(
                name: "wealth_health_snapshots");

            migrationBuilder.DropTable(
                name: "primary_stats");

            migrationBuilder.DropColumn(
                name: "dimension_scores",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "net_cash_flow",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "net_worth_current",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "net_worth_delta",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "savings_rate",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "total_expenses",
                table: "review_snapshots");

            migrationBuilder.DropColumn(
                name: "total_income",
                table: "review_snapshots");

            migrationBuilder.RenameColumn(
                name: "wealth_health_current",
                table: "review_snapshots",
                newName: "WealthHealthCurrent");

            migrationBuilder.RenameColumn(
                name: "primary_stats_current",
                table: "review_snapshots",
                newName: "PrimaryStatsCurrent");

            migrationBuilder.RenameColumn(
                name: "longevity_current",
                table: "review_snapshots",
                newName: "LongevityCurrent");

            migrationBuilder.RenameColumn(
                name: "health_index_current",
                table: "review_snapshots",
                newName: "HealthIndexCurrent");

            migrationBuilder.RenameColumn(
                name: "adherence_index_current",
                table: "review_snapshots",
                newName: "AdherenceIndexCurrent");

            migrationBuilder.RenameColumn(
                name: "linked_milestones",
                table: "identity_profiles",
                newName: "linked_milestone_ids");

            migrationBuilder.AlterColumn<decimal>(
                name: "WealthHealthCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldPrecision: 8,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryStatsCurrent",
                table: "review_snapshots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LongevityCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldPrecision: 8,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HealthIndexCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldPrecision: 8,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AdherenceIndexCurrent",
                table: "review_snapshots",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldPrecision: 8,
                oldScale: 4,
                oldNullable: true);
        }
    }
}

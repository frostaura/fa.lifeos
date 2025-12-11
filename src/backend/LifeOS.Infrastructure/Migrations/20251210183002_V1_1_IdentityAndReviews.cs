using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_1_IdentityAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveMisses",
                table: "streaks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPenaltyCalculatedAt",
                table: "streaks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskPenaltyScore",
                table: "streaks",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceAccountId",
                table: "simulation_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetAccountId",
                table: "simulation_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "identity_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    archetype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    archetype_description = table.Column<string>(type: "text", nullable: true),
                    values = table.Column<string>(type: "jsonb", nullable: false),
                    primary_stat_targets = table.Column<string>(type: "jsonb", nullable: false),
                    linked_milestone_ids = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_identity_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    response_data = table.Column<string>(type: "jsonb", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_onboarding_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_onboarding_responses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "primary_stat_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    strength = table.Column<int>(type: "integer", nullable: false),
                    wisdom = table.Column<int>(type: "integer", nullable: false),
                    charisma = table.Column<int>(type: "integer", nullable: false),
                    composure = table.Column<int>(type: "integer", nullable: false),
                    energy = table.Column<int>(type: "integer", nullable: false),
                    influence = table.Column<int>(type: "integer", nullable: false),
                    vitality = table.Column<int>(type: "integer", nullable: false),
                    calculation_details = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_primary_stat_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_primary_stat_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    health_index_delta = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    adherence_index_delta = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    wealth_health_delta = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    longevity_delta = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    top_streaks = table.Column<string>(type: "jsonb", nullable: true),
                    recommended_actions = table.Column<string>(type: "jsonb", nullable: true),
                    primary_stats_delta = table.Column<string>(type: "jsonb", nullable: true),
                    scenario_comparison = table.Column<string>(type: "jsonb", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_simulation_events_SourceAccountId",
                table: "simulation_events",
                column: "SourceAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_events_TargetAccountId",
                table: "simulation_events",
                column: "TargetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_identity_profiles_user_id",
                table: "identity_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_responses_user_id",
                table: "onboarding_responses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_primary_stat_records_user_id_recorded_at",
                table: "primary_stat_records",
                columns: new[] { "user_id", "recorded_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_review_snapshots_user_id_review_type_period_end",
                table: "review_snapshots",
                columns: new[] { "user_id", "review_type", "period_end" },
                descending: new[] { false, false, true });

            migrationBuilder.AddForeignKey(
                name: "FK_simulation_events_accounts_SourceAccountId",
                table: "simulation_events",
                column: "SourceAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_simulation_events_accounts_TargetAccountId",
                table: "simulation_events",
                column: "TargetAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_simulation_events_accounts_SourceAccountId",
                table: "simulation_events");

            migrationBuilder.DropForeignKey(
                name: "FK_simulation_events_accounts_TargetAccountId",
                table: "simulation_events");

            migrationBuilder.DropTable(
                name: "identity_profiles");

            migrationBuilder.DropTable(
                name: "onboarding_responses");

            migrationBuilder.DropTable(
                name: "primary_stat_records");

            migrationBuilder.DropTable(
                name: "review_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_simulation_events_SourceAccountId",
                table: "simulation_events");

            migrationBuilder.DropIndex(
                name: "IX_simulation_events_TargetAccountId",
                table: "simulation_events");

            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConsecutiveMisses",
                table: "streaks");

            migrationBuilder.DropColumn(
                name: "LastPenaltyCalculatedAt",
                table: "streaks");

            migrationBuilder.DropColumn(
                name: "RiskPenaltyScore",
                table: "streaks");

            migrationBuilder.DropColumn(
                name: "SourceAccountId",
                table: "simulation_events");

            migrationBuilder.DropColumn(
                name: "TargetAccountId",
                table: "simulation_events");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameFiAndNetWorthSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NetWorthSnapshots table
            migrationBuilder.CreateTable(
                name: "net_worth_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAssets = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalLiabilities = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    NetWorth = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    HomeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "ZAR"),
                    BreakdownByType = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    BreakdownByCurrency = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    AccountCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_net_worth_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_net_worth_snapshots_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_net_worth_snapshots_UserId_SnapshotDate",
                table: "net_worth_snapshots",
                columns: new[] { "UserId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_net_worth_snapshots_user_date_desc",
                table: "net_worth_snapshots",
                columns: new[] { "UserId", "SnapshotDate" },
                descending: new[] { false, true });

            // Achievements table
            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    XpValue = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "bronze"),
                    UnlockCondition = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievements_Code",
                table: "achievements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_Category",
                table: "achievements",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_IsActive",
                table: "achievements",
                column: "IsActive");

            // UserAchievements table
            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Progress = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    UnlockContext = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_achievements_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_achievements_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_UserId_AchievementId",
                table: "user_achievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_UserId",
                table: "user_achievements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_AchievementId",
                table: "user_achievements",
                column: "AchievementId");

            // UserXPs table
            migrationBuilder.CreateTable(
                name: "user_xps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalXp = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    WeeklyXp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_xps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_xps_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_xps_UserId",
                table: "user_xps",
                column: "UserId",
                unique: true);

            // Seed initial achievements
            SeedAchievements(migrationBuilder);
        }

        private void SeedAchievements(MigrationBuilder migrationBuilder)
        {
            // Financial Achievements
            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "first_account", "Getting Started", "Create your first account", "wallet", 10, "financial", "bronze", "accountCount >= 1", 1 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "net_worth_10k", "Building Wealth", "Reach R10,000 net worth", "trending-up", 50, "financial", "bronze", "netWorth >= 10000", 2 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "net_worth_100k", "Six Figures", "Reach R100,000 net worth", "star", 100, "financial", "silver", "netWorth >= 100000", 3 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "net_worth_1m", "Millionaire", "Reach R1,000,000 net worth", "trophy", 500, "financial", "gold", "netWorth >= 1000000", 4 });

            // Streak Achievements
            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "streak_7", "Week Warrior", "Maintain a 7-day streak", "flame", 25, "streak", "bronze", "maxStreak >= 7", 10 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "streak_30", "Monthly Master", "Maintain a 30-day streak", "flame", 100, "streak", "silver", "maxStreak >= 30", 11 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "streak_100", "Century Club", "Maintain a 100-day streak", "flame", 250, "streak", "gold", "maxStreak >= 100", 12 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "streak_365", "Year-Long Legend", "Maintain a 365-day streak", "crown", 1000, "streak", "platinum", "longestStreak >= 365", 13 });

            // Milestone Achievements
            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "first_milestone", "Goal Setter", "Complete your first milestone", "target", 25, "milestone", "bronze", "completedMilestones >= 1", 20 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "milestone_10", "Achievement Hunter", "Complete 10 milestones", "target", 100, "milestone", "silver", "completedMilestones >= 10", 21 });

            // Engagement Achievements
            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "first_metric", "Data Driven", "Log your first metric", "activity", 10, "engagement", "bronze", "metricRecords >= 1", 30 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "metric_100", "Tracker Pro", "Log 100 metrics", "activity", 50, "engagement", "silver", "metricRecords >= 100", 31 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "days_active_30", "Committed User", "Use LifeOS for 30 days", "calendar", 100, "engagement", "silver", "daysActive >= 30", 32 });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "Id", "Code", "Name", "Description", "Icon", "XpValue", "Category", "Tier", "UnlockCondition", "SortOrder" },
                values: new object[] { Guid.NewGuid(), "days_active_365", "Yearly Devotee", "Use LifeOS for a full year", "calendar", 500, "engagement", "gold", "daysActive >= 365", 33 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "user_xps");
            migrationBuilder.DropTable(name: "user_achievements");
            migrationBuilder.DropTable(name: "achievements");
            migrationBuilder.DropTable(name: "net_worth_snapshots");
        }
    }
}

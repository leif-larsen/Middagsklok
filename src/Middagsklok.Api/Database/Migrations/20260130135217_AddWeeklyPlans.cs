using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weekly_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weekly_plan_days",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    selection_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dish_id = table.Column<Guid>(type: "uuid", nullable: true),
                    weekly_plan_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_plan_days", x => x.id);
                    table.ForeignKey(
                        name: "FK_weekly_plan_days_weekly_plans_weekly_plan_id",
                        column: x => x.weekly_plan_id,
                        principalTable: "weekly_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_weekly_plan_days_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_plan_days_dish_id",
                table: "weekly_plan_days",
                column: "dish_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plan_days_weekly_plan_id_date",
                table: "weekly_plan_days",
                columns: new[] { "weekly_plan_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plans_start_date",
                table: "weekly_plans",
                column: "start_date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_plan_days");

            migrationBuilder.DropTable(
                name: "weekly_plans");
        }
    }
}

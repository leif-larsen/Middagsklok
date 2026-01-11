using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Migrations
{
    /// <inheritdoc />
    public partial class AddDishHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dish",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    active_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    total_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    kid_rating = table.Column<int>(type: "INTEGER", nullable: false),
                    family_rating = table.Column<int>(type: "INTEGER", nullable: false),
                    is_pescetarian = table.Column<bool>(type: "INTEGER", nullable: false),
                    has_optional_meat_variant = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ingredient",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: false),
                    default_unit = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredient", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weekly_plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    week_start_date = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_plan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dish_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    dish_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    rating_override = table.Column<int>(type: "INTEGER", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_dish_history_dish_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dish_ingredient",
                columns: table => new
                {
                    dish_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    unit = table.Column<string>(type: "TEXT", nullable: false),
                    optional = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_ingredient", x => new { x.dish_id, x.ingredient_id });
                    table.ForeignKey(
                        name: "FK_dish_ingredient_dish_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dish_ingredient_ingredient_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredient",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_plan_item",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    day_index = table.Column<int>(type: "INTEGER", nullable: false),
                    dish_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_plan_item", x => new { x.plan_id, x.day_index });
                    table.ForeignKey(
                        name: "FK_weekly_plan_item_dish_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_weekly_plan_item_weekly_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "weekly_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dish_name",
                table: "dish",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dish_history_dish_date",
                table: "dish_history",
                columns: new[] { "dish_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_dish_ingredient_ingredient_id",
                table: "dish_ingredient",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "IX_ingredient_name",
                table: "ingredient",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekly_plan_week_start_date",
                table: "weekly_plan",
                column: "week_start_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekly_plan_item_dish_id",
                table: "weekly_plan_item",
                column: "dish_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dish_history");

            migrationBuilder.DropTable(
                name: "dish_ingredient");

            migrationBuilder.DropTable(
                name: "weekly_plan_item");

            migrationBuilder.DropTable(
                name: "ingredient");

            migrationBuilder.DropTable(
                name: "dish");

            migrationBuilder.DropTable(
                name: "weekly_plan");
        }
    }
}

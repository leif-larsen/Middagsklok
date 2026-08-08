using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPortionScaling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "servings",
                table: "weekly_plan_days",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "household_size",
                table: "planning_settings",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "is_pantry_staple",
                table: "ingredients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "person_count",
                table: "dish_ingredients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scaling",
                table: "dish_ingredients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PerDish");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "servings",
                table: "weekly_plan_days");

            migrationBuilder.DropColumn(
                name: "household_size",
                table: "planning_settings");

            migrationBuilder.DropColumn(
                name: "is_pantry_staple",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "person_count",
                table: "dish_ingredients");

            migrationBuilder.DropColumn(
                name: "scaling",
                table: "dish_ingredients");
        }
    }
}

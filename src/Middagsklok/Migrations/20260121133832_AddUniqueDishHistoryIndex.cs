using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDishHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing non-unique index
            migrationBuilder.DropIndex(
                name: "ix_dish_history_dish_date",
                table: "dish_history");

            // Create unique index to prevent duplicate (dish_id, date) entries
            migrationBuilder.CreateIndex(
                name: "ix_dish_history_dish_date",
                table: "dish_history",
                columns: new[] { "dish_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop unique index
            migrationBuilder.DropIndex(
                name: "ix_dish_history_dish_date",
                table: "dish_history");

            // Restore non-unique index
            migrationBuilder.CreateIndex(
                name: "ix_dish_history_dish_date",
                table: "dish_history",
                columns: new[] { "dish_id", "date" });
        }
    }
}

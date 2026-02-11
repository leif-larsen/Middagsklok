using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDishVibeTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "vibe_tags",
                table: "dishes",
                type: "text[]",
                nullable: false,
                defaultValue: System.Array.Empty<string>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "vibe_tags",
                table: "dishes");
        }
    }
}

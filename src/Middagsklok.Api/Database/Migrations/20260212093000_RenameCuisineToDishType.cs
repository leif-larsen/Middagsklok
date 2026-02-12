using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260212093000_RenameCuisineToDishType")]
    /// <inheritdoc />
    public partial class RenameCuisineToDishType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "cuisine",
                table: "dishes",
                newName: "dish_type");

            migrationBuilder.RenameIndex(
                name: "ix_dishes_cuisine",
                table: "dishes",
                newName: "ix_dishes_dish_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "dish_type",
                table: "dishes",
                newName: "cuisine");

            migrationBuilder.RenameIndex(
                name: "ix_dishes_dish_type",
                table: "dishes",
                newName: "ix_dishes_cuisine");
        }
    }
}

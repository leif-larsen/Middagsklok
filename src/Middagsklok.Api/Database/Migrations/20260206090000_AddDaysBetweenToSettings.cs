using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260206090000_AddDaysBetweenToSettings")]
    /// <inheritdoc />
    public partial class AddDaysBetweenToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "days_between",
                table: "planning_settings",
                type: "integer",
                nullable: false,
                defaultValue: 14);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "days_between",
                table: "planning_settings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260809181837_AddDishRetiredAt")]
    /// <inheritdoc />
    public partial class AddDishRetiredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "retired_at",
                table: "dishes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "dishes");
        }
    }
}

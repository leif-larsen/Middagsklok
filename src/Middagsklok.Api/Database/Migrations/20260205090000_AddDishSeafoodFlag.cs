using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260205090000_AddDishSeafoodFlag")]
    /// <inheritdoc />
    public partial class AddDishSeafoodFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_seafood",
                table: "dishes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE dishes AS d
                SET is_seafood = TRUE
                WHERE EXISTS (
                    SELECT 1
                    FROM dish_ingredients AS di
                    INNER JOIN ingredients AS i ON i.id = di.ingredient_id
                    WHERE di.dish_id = d.id
                      AND i.category = 'Seafood'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_seafood",
                table: "dishes");
        }
    }
}

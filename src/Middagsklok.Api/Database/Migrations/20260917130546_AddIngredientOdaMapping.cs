using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middagsklok.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientOdaMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingredient_oda_products",
                columns: table => new
                {
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    availability = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    oda_product_id = table.Column<int>(type: "integer", nullable: true),
                    oda_product_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    pack_quantity = table.Column<double>(type: "double precision", nullable: true),
                    pack_unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredient_oda_products", x => x.ingredient_id);
                    table.ForeignKey(
                        name: "FK_ingredient_oda_products_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_oda_products_oda_product_id",
                table: "ingredient_oda_products",
                column: "oda_product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingredient_oda_products");
        }
    }
}

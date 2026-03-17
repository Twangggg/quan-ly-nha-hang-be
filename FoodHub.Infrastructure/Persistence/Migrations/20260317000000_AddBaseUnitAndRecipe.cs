using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    public partial class AddBaseUnitAndRecipe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "unit", table: "ingredients", newName: "base_unit");

            migrationBuilder.CreateTable(
                name: "ingredient_uom_conversions",
                columns: table => new
                {
                    ingredient_uom_conversion_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false
                    ),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_unit = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    to_unit = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    factor = table.Column<decimal>(
                        type: "numeric(18,6)",
                        precision: 18,
                        scale: 6,
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    deleted_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "pk_ingredient_uom_conversions",
                        x => x.ingredient_uom_conversion_id
                    );
                    table.ForeignKey(
                        name: "fk_ingredient_uom_conversions_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "menu_item_ingredients",
                columns: table => new
                {
                    menu_item_ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_per_serving = table.Column<decimal>(
                        type: "numeric(18,4)",
                        precision: 18,
                        scale: 4,
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    deleted_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item_ingredients", x => x.menu_item_ingredient_id);
                    table.ForeignKey(
                        name: "fk_menu_item_ingredients_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_menu_item_ingredients_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "menu_item_id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_uom_conversions_ingredient_id_from_unit_to_unit",
                table: "ingredient_uom_conversions",
                columns: new[] { "ingredient_id", "from_unit", "to_unit" },
                unique: true,
                filter: "deleted_at IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_uom_conversions_ingredient_id",
                table: "ingredient_uom_conversions",
                column: "ingredient_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_ingredients_menu_item_id_ingredient_id",
                table: "menu_item_ingredients",
                columns: new[] { "menu_item_id", "ingredient_id" },
                unique: true,
                filter: "deleted_at IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_ingredients_ingredient_id",
                table: "menu_item_ingredients",
                column: "ingredient_id"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ingredient_uom_conversions");

            migrationBuilder.DropTable(name: "menu_item_ingredients");

            migrationBuilder.RenameColumn(name: "base_unit", table: "ingredients", newName: "unit");
        }
    }
}

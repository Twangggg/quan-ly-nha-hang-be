using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMenuItemPriceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_menu_items_price_dine_in",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "price_dine_in",
                table: "menu_items");

            migrationBuilder.RenameColumn(
                name: "price_take_away",
                table: "menu_items",
                newName: "price");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_price",
                table: "menu_items",
                column: "price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_menu_items_price",
                table: "menu_items");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "menu_items",
                newName: "price_take_away");

            migrationBuilder.AddColumn<decimal>(
                name: "price_dine_in",
                table: "menu_items",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_price_dine_in",
                table: "menu_items",
                column: "price_dine_in");
        }
    }
}

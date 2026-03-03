using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKdsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_order_items_menu_items_menu_item_id",
                table: "order_items",
                column: "menu_item_id",
                principalTable: "menu_items",
                principalColumn: "menu_item_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_items_menu_items_menu_item_id",
                table: "order_items");
        }
    }
}

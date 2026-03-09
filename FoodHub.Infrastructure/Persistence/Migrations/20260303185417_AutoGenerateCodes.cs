using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AutoGenerateCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "set_menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "item_number",
                table: "set_menus",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "item_number",
                table: "menu_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Seed ItemNumber for existing menu_items
            migrationBuilder.Sql("UPDATE menu_items SET item_number = sub.rn FROM (SELECT menu_item_id, row_number() OVER (PARTITION BY category_id ORDER BY created_at) as rn FROM menu_items) as sub WHERE menu_items.menu_item_id = sub.menu_item_id");

            // Seed ItemNumber for existing set_menus (partition by NULL if needed)
            migrationBuilder.Sql("UPDATE set_menus SET item_number = sub.rn FROM (SELECT set_menu_id, row_number() OVER (PARTITION BY category_id ORDER BY created_at) as rn FROM set_menus) as sub WHERE set_menus.set_menu_id = sub.set_menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_category_id_item_number",
                table: "set_menus",
                columns: new[] { "category_id", "item_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_category_id_item_number",
                table: "menu_items",
                columns: new[] { "category_id", "item_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_set_menus_categories_category_id",
                table: "set_menus",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "category_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_set_menus_categories_category_id",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_set_menus_category_id_item_number",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_category_id_item_number",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "item_number",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "item_number",
                table: "menu_items");
        }
    }
}

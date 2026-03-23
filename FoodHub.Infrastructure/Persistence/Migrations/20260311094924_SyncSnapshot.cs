using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop columns/indexes that might still exist but are being replaced
            // Note: Many of these were supposed to be handled by previous migrations but were out of sync in Snapshot.

            /*
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

            migrationBuilder.AddColumn<string>(
                name: "code_prefix",
                table: "categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "areas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
            */


            /*
            // New Indexes
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

            migrationBuilder.CreateIndex(
                name: "ix_categories_code_prefix",
                table: "categories",
                column: "code_prefix",
                unique: true,
                filter: "deleted_at IS NULL");
            */

            migrationBuilder.CreateIndex(
                name: "ix_reservations_area_id",
                table: "reservations",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_table_id",
                table: "reservations",
                column: "table_id");

            /*
            migrationBuilder.AddForeignKey(
                name: "fk_set_menus_categories_category_id",
                table: "set_menus",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "category_id",
                onDelete: ReferentialAction.Restrict);
            */
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

            migrationBuilder.DropIndex(
                name: "ix_categories_code_prefix",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "item_number",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "item_number",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "code_prefix",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "type",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "description",
                table: "areas");
        }
    }
}

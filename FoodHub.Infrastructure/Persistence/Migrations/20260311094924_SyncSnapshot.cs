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

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reservation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reservation_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    party_type = table.Column<int>(type: "integer", nullable: false),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    has_children = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.reservation_id);
                    table.ForeignKey(
                        name: "fk_reservations_areas_area_id",
                        column: x => x.area_id,
                        principalTable: "areas",
                        principalColumn: "area_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reservations_tables_table_id",
                        column: x => x.table_id,
                        principalTable: "tables",
                        principalColumn: "table_id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.DropTable(
                name: "reservations");

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

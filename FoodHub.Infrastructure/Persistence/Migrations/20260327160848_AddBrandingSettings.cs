using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_items_menu_items_menu_item_id",
                table: "order_items");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_item_id",
                table: "order_items",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "branding_settings",
                columns: table => new
                {
                    branding_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settings_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    restaurant_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    date_format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    bill_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bill_footer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    kds_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    app_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branding_settings", x => x.branding_settings_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branding_settings_settings_key",
                table: "branding_settings",
                column: "settings_key",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_order_items_menu_items_menu_item_id",
                table: "order_items",
                column: "menu_item_id",
                principalTable: "menu_items",
                principalColumn: "menu_item_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_items_menu_items_menu_item_id",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "branding_settings");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_item_id",
                table: "order_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_order_items_menu_items_menu_item_id",
                table: "order_items",
                column: "menu_item_id",
                principalTable: "menu_items",
                principalColumn: "menu_item_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

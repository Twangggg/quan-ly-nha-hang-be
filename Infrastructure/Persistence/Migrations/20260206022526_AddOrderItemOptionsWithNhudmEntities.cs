using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemOptionsWithNhudmEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "set_menu_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "set_menu_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "set_menu_items");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "set_menu_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "set_menu_items");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "set_menu_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "categories");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "menu_items",
                newName: "updated_by_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "menu_items",
                newName: "created_by_id");

            migrationBuilder.CreateTable(
                name: "order_item_option_groups",
                columns: table => new
                {
                    order_item_option_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group_type_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_required_snapshot = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_option_groups", x => x.order_item_option_group_id);
                    table.ForeignKey(
                        name: "fk_order_item_option_groups_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "order_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_item_option_values",
                columns: table => new
                {
                    order_item_option_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_option_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    label_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    extra_price_snapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_option_values", x => x.order_item_option_value_id);
                    table.ForeignKey(
                        name: "fk_order_item_option_values_group_id",
                        column: x => x.order_item_option_group_id,
                        principalTable: "order_item_option_groups",
                        principalColumn: "order_item_option_group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_item_option_values_option_item_id",
                        column: x => x.option_item_id,
                        principalTable: "option_items",
                        principalColumn: "option_item_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_order_item_option_groups_order_item_id",
                table: "order_item_option_groups",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_item_option_values_group_id",
                table: "order_item_option_values",
                column: "order_item_option_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_option_values_option_item_id",
                table: "order_item_option_values",
                column: "option_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_option_values");

            migrationBuilder.DropTable(
                name: "order_item_option_groups");

            migrationBuilder.RenameColumn(
                name: "updated_by_id",
                table: "menu_items",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_id",
                table: "menu_items",
                newName: "created_by");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "set_menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "set_menus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "set_menus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "set_menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "set_menu_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "set_menu_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "set_menu_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "set_menu_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "set_menu_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "set_menu_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "option_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "option_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "option_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "option_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "option_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "option_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "option_groups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "option_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "option_groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "option_groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "option_groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "option_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "menu_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "categories",
                type: "uuid",
                nullable: true);
        }
    }
}

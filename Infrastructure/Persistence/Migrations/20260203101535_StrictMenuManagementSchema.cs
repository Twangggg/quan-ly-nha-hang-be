using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class StrictMenuManagementSchema : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

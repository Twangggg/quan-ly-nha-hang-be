using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaBasedOnEntityChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "updated_by_id",
                table: "menu_items",
                newName: "updated_by_employee_id");

            migrationBuilder.RenameColumn(
                name: "created_by_id",
                table: "menu_items",
                newName: "created_by_employee_id");

            migrationBuilder.RenameColumn(
                name: "cost",
                table: "menu_items",
                newName: "cost_price");

            migrationBuilder.AddColumn<decimal>(
                name: "cost_price",
                table: "set_menus",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "created_by_employee_id",
                table: "set_menus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "set_menus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "set_menus",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "set_menus",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "set_type",
                table: "set_menus",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "updated_by_employee_id",
                table: "set_menus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "set_menu_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "option_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "option_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "option_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "option_groups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "option_groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "option_groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_price",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "created_by_employee_id",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "description",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "set_type",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "updated_by_employee_id",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "set_menu_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "categories");

            migrationBuilder.RenameColumn(
                name: "updated_by_employee_id",
                table: "menu_items",
                newName: "updated_by_id");

            migrationBuilder.RenameColumn(
                name: "created_by_employee_id",
                table: "menu_items",
                newName: "created_by_id");

            migrationBuilder.RenameColumn(
                name: "cost_price",
                table: "menu_items",
                newName: "cost");
        }
    }
}

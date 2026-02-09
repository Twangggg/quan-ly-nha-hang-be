using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEntityInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by_employee_id",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "updated_by_employee_id",
                table: "set_menus");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "set_menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "set_menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "option_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "option_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "option_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "option_groups",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "option_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "option_groups");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "option_groups");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_employee_id",
                table: "set_menus",
                type: "uuid",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_employee_id",
                table: "set_menus",
                type: "uuid",
                maxLength: 50,
                nullable: true);
        }
    }
}

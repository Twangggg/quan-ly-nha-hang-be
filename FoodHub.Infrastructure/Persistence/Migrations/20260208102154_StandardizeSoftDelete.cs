using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_employees_created_by",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "updated_by_employee_id",
                table: "menu_items",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_employee_id",
                table: "menu_items",
                newName: "created_by");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                table: "orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_employees_created_by",
                table: "orders",
                column: "created_by",
                principalTable: "employees",
                principalColumn: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_employees_created_by",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "menu_items",
                newName: "updated_by_employee_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "menu_items",
                newName: "created_by_employee_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                table: "orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_employees_created_by",
                table: "orders",
                column: "created_by",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

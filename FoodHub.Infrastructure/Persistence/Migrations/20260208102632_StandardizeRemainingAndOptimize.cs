using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeRemainingAndOptimize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_employees_role",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_status",
                table: "employees");

            migrationBuilder.RenameColumn(
                name: "delete_at",
                table: "employees",
                newName: "deleted_at");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "employees",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_status_created_at",
                table: "orders",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_status_created_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "categories");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "employees",
                newName: "delete_at");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "employees",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "ix_employees_role",
                table: "employees",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_employees_status",
                table: "employees",
                column: "status");
        }
    }
}

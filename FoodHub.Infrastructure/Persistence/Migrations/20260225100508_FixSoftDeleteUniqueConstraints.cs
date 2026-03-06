using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class FixSoftDeleteUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_set_menus_code",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_code",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_employees_email",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_employee_code",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_phone",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_username",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_categories_name",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_code",
                table: "set_menus",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_code",
                table: "menu_items",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employees_email",
                table: "employees",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employees_employee_code",
                table: "employees",
                column: "employee_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employees_phone",
                table: "employees",
                column: "phone",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employees_username",
                table: "employees",
                column: "username",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_categories_name",
                table: "categories",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_set_menus_code",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_code",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_employees_email",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_employee_code",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_phone",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_username",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_categories_name",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_code",
                table: "set_menus",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_code",
                table: "menu_items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_employee_code",
                table: "employees",
                column: "employee_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_phone",
                table: "employees",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_username",
                table: "employees",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_name",
                table: "categories",
                column: "name",
                unique: true);
        }
    }
}

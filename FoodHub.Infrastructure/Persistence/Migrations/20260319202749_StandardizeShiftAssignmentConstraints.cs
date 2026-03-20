using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeShiftAssignmentConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignments_employees_employee_id",
                table: "shift_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignments_shifts_shift_id",
                table: "shift_assignments");

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignments_employee_id",
                table: "shift_assignments",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignments_shift_id",
                table: "shift_assignments",
                column: "shift_id",
                principalTable: "shifts",
                principalColumn: "shift_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignments_employee_id",
                table: "shift_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignments_shift_id",
                table: "shift_assignments");

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignments_employees_employee_id",
                table: "shift_assignments",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignments_shifts_shift_id",
                table: "shift_assignments",
                column: "shift_id",
                principalTable: "shifts",
                principalColumn: "shift_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

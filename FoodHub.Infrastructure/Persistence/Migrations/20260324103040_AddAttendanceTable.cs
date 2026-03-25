using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendances",
                columns: table => new
                {
                    attendance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    check_in_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    check_out_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_late = table.Column<bool>(type: "boolean", nullable: true),
                    is_early_leave = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendances", x => x.attendance_id);
                    table.ForeignKey(
                        name: "fk_attendances_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendances_shift_assignment_id",
                        column: x => x.shift_assignment_id,
                        principalTable: "shift_assignments",
                        principalColumn: "shift_assignment_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendances_employee_id",
                table: "attendances",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendances_shift_assignment_id",
                table: "attendances",
                column: "shift_assignment_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendances");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    role = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.employee_id);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_logs",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reset_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "fk_password_reset_logs_employees_performed_by_employee_id",
                        column: x => x.performed_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_password_reset_logs_employees_target_employee_id",
                        column: x => x.target_employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "ix_employees_role",
                table: "employees",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_employees_status",
                table: "employees",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_employees_username",
                table: "employees",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_logs_performed_by_employee_id",
                table: "password_reset_logs",
                column: "performed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_logs_reset_at",
                table: "password_reset_logs",
                column: "reset_at");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_logs_target_employee_id",
                table: "password_reset_logs",
                column: "target_employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_logs");

            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}

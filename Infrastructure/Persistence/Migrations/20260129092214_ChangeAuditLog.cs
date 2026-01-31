using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_logs");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<short>(type: "smallint", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "fk_audit_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "fk_audit_logs_employees_employee_id1",
                        column: x => x.employee_id1,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "fk_audit_logs_employees_performed_by_employee_id",
                        column: x => x.performed_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id",
                table: "audit_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id1",
                table: "audit_logs",
                column: "employee_id1");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_performed_by_employee_id",
                table: "audit_logs",
                column: "performed_by_employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.CreateTable(
                name: "password_reset_logs",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
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
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_employees_employee_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_employees_employee_id1",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_employee_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_employee_id1",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "employee_id1",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_target_id",
                table: "audit_logs",
                column: "target_id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_employees_target_id",
                table: "audit_logs",
                column: "target_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_employees_target_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_target_id",
                table: "audit_logs");

            migrationBuilder.AddColumn<Guid>(
                name: "employee_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "employee_id1",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id",
                table: "audit_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id1",
                table: "audit_logs",
                column: "employee_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_employees_employee_id",
                table: "audit_logs",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "employee_id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_employees_employee_id1",
                table: "audit_logs",
                column: "employee_id1",
                principalTable: "employees",
                principalColumn: "employee_id");
        }
    }
}

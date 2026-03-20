using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAuditLogGeneric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_employees_performed_by_employee_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_employees_target_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_performed_by_employee_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_target_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "performed_by_employee_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "target_id",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "reason",
                table: "audit_logs",
                newName: "actor_info");

            migrationBuilder.RenameColumn(
                name: "metadata",
                table: "audit_logs",
                newName: "old_values");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "audit_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

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

            migrationBuilder.AddColumn<string>(
                name: "entity_id",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "entity_name",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_values",
                table: "audit_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id",
                table: "audit_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id1",
                table: "audit_logs",
                column: "employee_id1");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_name_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_name", "entity_id" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_employees_employee_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_employees_employee_id1",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_employee_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_employee_id1",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_entity_name_entity_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "employee_id1",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "entity_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "entity_name",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "new_values",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "old_values",
                table: "audit_logs",
                newName: "metadata");

            migrationBuilder.RenameColumn(
                name: "actor_info",
                table: "audit_logs",
                newName: "reason");

            migrationBuilder.AlterColumn<short>(
                name: "action",
                table: "audit_logs",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "performed_by_employee_id",
                table: "audit_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                table: "audit_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_performed_by_employee_id",
                table: "audit_logs",
                column: "performed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_target_id",
                table: "audit_logs",
                column: "target_id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_employees_performed_by_employee_id",
                table: "audit_logs",
                column: "performed_by_employee_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_employees_target_id",
                table: "audit_logs",
                column: "target_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

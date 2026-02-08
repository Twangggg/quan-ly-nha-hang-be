using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class SyncLatestChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE set_menus ALTER COLUMN updated_by_employee_id TYPE uuid USING updated_by_employee_id::uuid");
            migrationBuilder.Sql("ALTER TABLE set_menus ALTER COLUMN created_by_employee_id TYPE uuid USING created_by_employee_id::uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "updated_by_employee_id",
                table: "set_menus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by_employee_id",
                table: "set_menus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}

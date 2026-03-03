using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tables_table_code",
                table: "tables");

            migrationBuilder.DropIndex(
                name: "idx_areas_code_prefix",
                table: "areas");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "areas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "idx_tables_table_code",
                table: "tables",
                column: "table_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_areas_code_prefix",
                table: "areas",
                column: "code_prefix",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tables_table_code",
                table: "tables");

            migrationBuilder.DropIndex(
                name: "idx_areas_code_prefix",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "description",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "type",
                table: "areas");

            migrationBuilder.CreateIndex(
                name: "idx_tables_table_code",
                table: "tables",
                column: "table_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_areas_code_prefix",
                table: "areas",
                column: "code_prefix");
        }
    }
}

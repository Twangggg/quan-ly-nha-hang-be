using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModifyTableEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tables_table_code",
                table: "tables");

            migrationBuilder.DropColumn(
                name: "table_code",
                table: "tables");

            migrationBuilder.AddColumn<int>(
                name: "table_number",
                table: "tables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "idx_tables_table_number",
                table: "tables",
                column: "table_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tables_table_number",
                table: "tables");

            migrationBuilder.DropColumn(
                name: "table_number",
                table: "tables");

            migrationBuilder.AddColumn<string>(
                name: "table_code",
                table: "tables",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "idx_tables_table_code",
                table: "tables",
                column: "table_code",
                unique: true);
        }
    }
}

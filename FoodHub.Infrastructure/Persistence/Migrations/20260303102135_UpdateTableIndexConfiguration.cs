using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableIndexConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tables_table_number",
                table: "tables");

            migrationBuilder.CreateIndex(
                name: "idx_tables_table_number",
                table: "tables",
                columns: new[] { "table_number", "area_id" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tables_table_number",
                table: "tables");

            migrationBuilder.CreateIndex(
                name: "idx_tables_table_number",
                table: "tables",
                column: "table_number");
        }
    }
}

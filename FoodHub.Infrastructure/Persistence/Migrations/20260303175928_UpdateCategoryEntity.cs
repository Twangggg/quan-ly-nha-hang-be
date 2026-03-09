using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code_prefix",
                table: "categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            // Populate with temporary unique values
            migrationBuilder.Sql("UPDATE categories SET code_prefix = LEFT(category_id::text, 10)");

            migrationBuilder.AlterColumn<string>(
                name: "code_prefix",
                table: "categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ix_categories_code_prefix",
                table: "categories",
                column: "code_prefix",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_code_prefix",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "code_prefix",
                table: "categories");
        }
    }
}

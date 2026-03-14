using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIngredientStockStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE IF EXISTS \"ingredients\" DROP COLUMN IF EXISTS \"stock_status\";"
            );
            migrationBuilder.Sql(
                "ALTER TABLE IF EXISTS \"ingredients\" DROP COLUMN IF EXISTS \"status\";"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stock_status",
                table: "ingredients",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "ingredients",
                type: "text",
                nullable: true
            );
        }
    }
}

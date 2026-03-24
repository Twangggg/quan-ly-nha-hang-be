using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    public partial class AddIngredientDefaultThresholdFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "use_default_low_stock_threshold",
                table: "ingredients",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "use_default_low_stock_threshold",
                table: "ingredients"
            );
        }
    }
}

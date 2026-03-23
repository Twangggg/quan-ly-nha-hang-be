using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    public partial class MigrateReadyOrderItemsToCompleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE order_items
                SET status = 4,
                    updated_at = COALESCE(updated_at, NOW())
                WHERE status = 3;
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE order_items
                SET status = 3
                WHERE status = 4 AND stock_deducted = FALSE;
                """
            );
        }
    }
}

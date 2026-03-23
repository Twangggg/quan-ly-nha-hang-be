using FoodHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260323193000_RenumberOrderItemStatuses")]
    public partial class RenumberOrderItemStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE order_items
                SET status = CASE
                    WHEN status = 4 THEN 3
                    WHEN status = 5 THEN 4
                    WHEN status = 6 THEN 5
                    ELSE status
                END,
                    updated_at = COALESCE(updated_at, NOW())
                WHERE status IN (4, 5, 6);
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE order_items
                SET status = CASE
                    WHEN status = 3 THEN 4
                    WHEN status = 4 THEN 5
                    WHEN status = 5 THEN 6
                    ELSE status
                END,
                    updated_at = COALESCE(updated_at, NOW())
                WHERE status IN (3, 4, 5);
                """
            );
        }
    }
}

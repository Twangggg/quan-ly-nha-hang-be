using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260327100000_AddUpcomingBufferToSettings")]
    public partial class AddUpcomingBufferToSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "upcoming_buffer_minutes",
                table: "reservation_settings",
                type: "integer",
                nullable: false,
                defaultValue: 30
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "upcoming_buffer_minutes",
                table: "reservation_settings"
            );
        }
    }
}

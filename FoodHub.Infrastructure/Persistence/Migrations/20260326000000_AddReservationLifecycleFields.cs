using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    public partial class AddReservationLifecycleFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "grace_period_minutes",
                table: "reservation_settings",
                type: "integer",
                nullable: false,
                defaultValue: 15
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "checked_in_at",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grace_period_minutes",
                table: "reservation_settings"
            );

            migrationBuilder.DropColumn(name: "checked_in_at", table: "reservations");
        }
    }
}

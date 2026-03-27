using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260326000000_AddReservationLifecycleFields")]
    public partial class AddReservationLifecycleFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE reservation_settings ADD COLUMN IF NOT EXISTS grace_period_minutes integer NOT NULL DEFAULT 15");
            migrationBuilder.Sql("ALTER TABLE reservations ADD COLUMN IF NOT EXISTS checked_in_at timestamp with time zone NULL");
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

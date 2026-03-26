using System;
using FoodHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260325040000_AddReservationSettings")]
    public partial class AddReservationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservation_settings",
                columns: table => new
                {
                    reservation_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    open_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    close_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    break_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    break_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    break_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    settings_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    overlap_buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    min_lead_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_settings", x => x.reservation_settings_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reservation_settings_settings_key",
                table: "reservation_settings",
                column: "settings_key",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservation_settings");
        }
    }
}

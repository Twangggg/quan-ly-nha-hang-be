using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncAttendanceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kds_settings",
                columns: table => new
                {
                    kds_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settings_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sort_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    wait_time_per_minute = table.Column<double>(type: "double precision", nullable: false),
                    order_priority_bonus = table.Column<double>(type: "double precision", nullable: false),
                    expected_time_weight = table.Column<double>(type: "double precision", nullable: false),
                    overdue_per_minute = table.Column<double>(type: "double precision", nullable: false),
                    completion_boost_weight = table.Column<double>(type: "double precision", nullable: false),
                    takeaway_bonus = table.Column<double>(type: "double precision", nullable: false),
                    delivery_bonus = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kds_settings", x => x.kds_settings_id);
                });

            migrationBuilder.CreateTable(
                name: "kds_station_wip_limits",
                columns: table => new
                {
                    kds_station_wip_limit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kds_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    limit = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kds_station_wip_limits", x => x.kds_station_wip_limit_id);
                    table.ForeignKey(
                        name: "fk_kds_station_wip_limits_kds_settings_kds_settings_id",
                        column: x => x.kds_settings_id,
                        principalTable: "kds_settings",
                        principalColumn: "kds_settings_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_kds_settings_settings_key",
                table: "kds_settings",
                column: "settings_key",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_kds_station_wip_limits_kds_settings_id_station",
                table: "kds_station_wip_limits",
                columns: new[] { "kds_settings_id", "station" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kds_station_wip_limits");

            migrationBuilder.DropTable(
                name: "kds_settings");
        }
    }
}

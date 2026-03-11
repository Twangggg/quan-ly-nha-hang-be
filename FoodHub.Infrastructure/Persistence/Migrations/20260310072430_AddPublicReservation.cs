using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "area",
                table: "tables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reservation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reservation_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    party_type = table.Column<int>(type: "integer", nullable: false),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    has_children = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.reservation_id);
                    table.ForeignKey(
                        name: "fk_reservations_tables_table_id",
                        column: x => x.table_id,
                        principalTable: "tables",
                        principalColumn: "table_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_table_id",
                table: "reservations",
                column: "table_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropColumn(
                name: "area",
                table: "tables");
        }
    }
}

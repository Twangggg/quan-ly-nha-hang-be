using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBeforeSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "area_id",
                table: "reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reservations_area_id",
                table: "reservations",
                column: "area_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reservations_areas_area_id",
                table: "reservations",
                column: "area_id",
                principalTable: "areas",
                principalColumn: "area_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reservations_areas_area_id",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "ix_reservations_area_id",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "area_id",
                table: "reservations");
        }
    }
}

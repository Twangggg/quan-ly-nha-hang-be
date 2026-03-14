using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationIdToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reservation_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_reservation_id",
                table: "orders",
                column: "reservation_id",
                unique: true,
                filter: "reservation_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_reservations_reservation_id",
                table: "orders",
                column: "reservation_id",
                principalTable: "reservations",
                principalColumn: "reservation_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_reservations_reservation_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_reservation_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "reservation_id",
                table: "orders");
        }
    }
}

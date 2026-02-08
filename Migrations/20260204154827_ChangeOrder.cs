using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "orders");

            migrationBuilder.AddColumn<DateTime>(
                name: "canceled_at",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "order_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canceled_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "status",
                table: "order_items");

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}

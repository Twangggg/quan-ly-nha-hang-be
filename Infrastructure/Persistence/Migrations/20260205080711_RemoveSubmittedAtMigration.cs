using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubmittedAtMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "orders");

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
                name: "status",
                table: "order_items");

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}

using System;
using FoodHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260325033000_AddOpeningStockImportCooldown")]
    public partial class AddOpeningStockImportCooldown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE inventory_settings
                ADD COLUMN IF NOT EXISTS opening_stock_import_cooldown_hours integer NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE inventory_settings
                ADD COLUMN IF NOT EXISTS last_opening_stock_imported_at timestamp with time zone NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "opening_stock_import_cooldown_hours",
                table: "inventory_settings");

            migrationBuilder.DropColumn(
                name: "last_opening_stock_imported_at",
                table: "inventory_settings");
        }
    }
}

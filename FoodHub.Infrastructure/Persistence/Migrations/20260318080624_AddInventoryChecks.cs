using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "receipt_type",
                table: "stock_out_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "receipt_type",
                table: "stock_in_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "inventory_checks",
                columns: table => new
                {
                    inventory_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_checks", x => x.inventory_check_id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_check_items",
                columns: table => new
                {
                    inventory_check_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    physical_quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    difference_quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_check_items", x => x.inventory_check_item_id);
                    table.ForeignKey(
                        name: "fk_inventory_check_items_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_check_items_inventory_checks_inventory_check_id",
                        column: x => x.inventory_check_id,
                        principalTable: "inventory_checks",
                        principalColumn: "inventory_check_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_check_items_ingredient_id",
                table: "inventory_check_items",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_check_items_inventory_check_id",
                table: "inventory_check_items",
                column: "inventory_check_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_checks_check_date",
                table: "inventory_checks",
                column: "check_date");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_checks_status",
                table: "inventory_checks",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_check_items");

            migrationBuilder.DropTable(
                name: "inventory_checks");

            migrationBuilder.DropColumn(
                name: "receipt_type",
                table: "stock_out_receipts");

            migrationBuilder.DropColumn(
                name: "receipt_type",
                table: "stock_in_receipts");
        }
    }
}

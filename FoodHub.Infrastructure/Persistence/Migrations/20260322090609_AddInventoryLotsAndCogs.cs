using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLotsAndCogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cost_calculated_at",
                table: "stock_out_receipt_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cost_calculation_source",
                table: "stock_out_receipt_items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "inventory_lots",
                columns: table => new
                {
                    inventory_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_in_receipt_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    original_quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_lots", x => x.inventory_lot_id);
                    table.ForeignKey(
                        name: "fk_inventory_lots_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_lots_stock_in_receipt_items_stock_in_receipt_item",
                        column: x => x.stock_in_receipt_item_id,
                        principalTable: "stock_in_receipt_items",
                        principalColumn: "stock_in_receipt_item_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_lot_movements",
                columns: table => new
                {
                    inventory_lot_movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_lot_movements", x => x.inventory_lot_movement_id);
                    table.ForeignKey(
                        name: "fk_inventory_lot_movements_inventory_lots_inventory_lot_id",
                        column: x => x.inventory_lot_id,
                        principalTable: "inventory_lots",
                        principalColumn: "inventory_lot_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_out_receipt_item_lot_allocations",
                columns: table => new
                {
                    stock_out_receipt_item_lot_allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_out_receipt_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_out_receipt_item_lot_allocations", x => x.stock_out_receipt_item_lot_allocation_id);
                    table.ForeignKey(
                        name: "fk_stock_out_receipt_item_lot_allocations_inventory_lots_inven",
                        column: x => x.inventory_lot_id,
                        principalTable: "inventory_lots",
                        principalColumn: "inventory_lot_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_out_receipt_item_lot_allocations_stock_out_receipt_it",
                        column: x => x.stock_out_receipt_item_id,
                        principalTable: "stock_out_receipt_items",
                        principalColumn: "stock_out_receipt_item_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lot_movements_inventory_lot_id_occurred_at",
                table: "inventory_lot_movements",
                columns: new[] { "inventory_lot_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lot_movements_reference_id_reference_type",
                table: "inventory_lot_movements",
                columns: new[] { "reference_id", "reference_type" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lots_expiry_date",
                table: "inventory_lots",
                column: "expiry_date");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lots_ingredient_id_lot_code",
                table: "inventory_lots",
                columns: new[] { "ingredient_id", "lot_code" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lots_ingredient_id_remaining_quantity",
                table: "inventory_lots",
                columns: new[] { "ingredient_id", "remaining_quantity" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lots_ingredient_id_status_expiry_date",
                table: "inventory_lots",
                columns: new[] { "ingredient_id", "status", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lots_stock_in_receipt_item_id",
                table: "inventory_lots",
                column: "stock_in_receipt_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_out_receipt_item_lot_allocations_inventory_lot_id",
                table: "stock_out_receipt_item_lot_allocations",
                column: "inventory_lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_out_receipt_item_lot_allocations_stock_out_receipt_it",
                table: "stock_out_receipt_item_lot_allocations",
                column: "stock_out_receipt_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_lot_movements");

            migrationBuilder.DropTable(
                name: "stock_out_receipt_item_lot_allocations");

            migrationBuilder.DropTable(
                name: "inventory_lots");

            migrationBuilder.DropColumn(
                name: "cost_calculated_at",
                table: "stock_out_receipt_items");

            migrationBuilder.DropColumn(
                name: "cost_calculation_source",
                table: "stock_out_receipt_items");
        }
    }
}

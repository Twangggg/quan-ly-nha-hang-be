using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockInReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_in_receipts",
                columns: table => new
                {
                    stock_in_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_lines = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_in_receipts", x => x.stock_in_receipt_id);
                });

            migrationBuilder.CreateTable(
                name: "stock_in_receipt_items",
                columns: table => new
                {
                    stock_in_receipt_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_in_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    line_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    batch_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_in_receipt_items", x => x.stock_in_receipt_item_id);
                    table.ForeignKey(
                        name: "fk_stock_in_receipt_items_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_in_receipt_items_stock_in_receipts_stock_in_receipt_id",
                        column: x => x.stock_in_receipt_id,
                        principalTable: "stock_in_receipts",
                        principalColumn: "stock_in_receipt_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_in_receipt_items_ingredient_id",
                table: "stock_in_receipt_items",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_in_receipt_items_stock_in_receipt_id_ingredient_id",
                table: "stock_in_receipt_items",
                columns: new[] { "stock_in_receipt_id", "ingredient_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_in_receipts_receipt_code",
                table: "stock_in_receipts",
                column: "receipt_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_in_receipts_received_at",
                table: "stock_in_receipts",
                column: "received_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_in_receipt_items");

            migrationBuilder.DropTable(
                name: "stock_in_receipts");
        }
    }
}

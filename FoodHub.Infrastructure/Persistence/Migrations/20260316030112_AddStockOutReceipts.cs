using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockOutReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_out_receipts",
                columns: table => new
                {
                    stock_out_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stock_out_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_out_receipts", x => x.stock_out_receipt_id);
                });

            migrationBuilder.CreateTable(
                name: "stock_out_receipt_items",
                columns: table => new
                {
                    stock_out_receipt_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_out_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    line_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_out_receipt_items", x => x.stock_out_receipt_item_id);
                    table.ForeignKey(
                        name: "fk_stock_out_receipt_items_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_out_receipt_items_stock_out_receipts_stock_out_receip",
                        column: x => x.stock_out_receipt_id,
                        principalTable: "stock_out_receipts",
                        principalColumn: "stock_out_receipt_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_out_receipt_items_ingredient_id",
                table: "stock_out_receipt_items",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_out_receipt_items_stock_out_receipt_id_ingredient_id",
                table: "stock_out_receipt_items",
                columns: new[] { "stock_out_receipt_id", "ingredient_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_out_receipts_receipt_code",
                table: "stock_out_receipts",
                column: "receipt_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_out_receipts_stock_out_date",
                table: "stock_out_receipts",
                column: "stock_out_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_out_receipt_items");

            migrationBuilder.DropTable(
                name: "stock_out_receipts");
        }
    }
}

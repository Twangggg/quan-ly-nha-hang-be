using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "voucher_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_free_item",
                table: "order_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "vouchers",
                columns: table => new
                {
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    voucher_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    min_order_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    free_quantity = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    used_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vouchers", x => x.voucher_id);
                    table.ForeignKey(
                        name: "fk_vouchers_menu_items_item_id",
                        column: x => x.item_id,
                        principalTable: "menu_items",
                        principalColumn: "menu_item_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_voucher_id",
                table: "orders",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_code",
                table: "vouchers",
                column: "voucher_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_item_id",
                table: "vouchers",
                column: "item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_vouchers_voucher_id",
                table: "orders",
                column: "voucher_id",
                principalTable: "vouchers",
                principalColumn: "voucher_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_vouchers_voucher_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "vouchers");

            migrationBuilder.DropIndex(
                name: "ix_orders_voucher_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "voucher_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_free_item",
                table: "order_items");
        }
    }
}

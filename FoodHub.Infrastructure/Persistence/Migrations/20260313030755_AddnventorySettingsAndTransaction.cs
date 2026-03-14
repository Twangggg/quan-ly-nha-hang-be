using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddnventorySettingsAndTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_settings",
                columns: table => new
                {
                    inventory_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settings_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expiry_warning_days = table.Column<int>(type: "integer", nullable: false),
                    default_low_stock_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    auto_deduct_on_completed = table.Column<bool>(type: "boolean", nullable: false),
                    cost_method = table.Column<int>(type: "integer", nullable: false),
                    max_cost_recalc_days = table.Column<int>(type: "integer", nullable: false),
                    opening_stock_status = table.Column<int>(type: "integer", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_settings", x => x.inventory_settings_id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                columns: table => new
                {
                    inventory_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_transactions", x => x.inventory_transaction_id);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_settings_settings_key",
                table: "inventory_settings",
                column: "settings_key",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_ingredient_id",
                table: "inventory_transactions",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_occurred_at",
                table: "inventory_transactions",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_transaction_type",
                table: "inventory_transactions",
                column: "transaction_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_settings");

            migrationBuilder.DropTable(
                name: "inventory_transactions");
        }
    }
}

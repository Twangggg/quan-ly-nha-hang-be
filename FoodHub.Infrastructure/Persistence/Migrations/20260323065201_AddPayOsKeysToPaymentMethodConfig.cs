using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOsKeysToPaymentMethodConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_method_configs",
                columns: table => new
                {
                    payment_method_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_bin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    account_holder_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pay_os_client_id = table.Column<string>(type: "text", nullable: true),
                    pay_os_api_key = table.Column<string>(type: "text", nullable: true),
                    pay_os_checksum_key = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_method_configs", x => x.payment_method_config_id);
                });

            migrationBuilder.CreateTable(
                name: "order_payments",
                columns: table => new
                {
                    order_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_payments", x => x.order_payment_id);
                    table.ForeignKey(
                        name: "fk_order_payments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_payments_payment_method_configs_payment_method_config",
                        column: x => x.payment_method_config_id,
                        principalTable: "payment_method_configs",
                        principalColumn: "payment_method_config_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_payments_order_id",
                table: "order_payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_payments_paid_at",
                table: "order_payments",
                column: "paid_at");

            migrationBuilder.CreateIndex(
                name: "ix_order_payments_payment_method_config_id",
                table: "order_payments",
                column: "payment_method_config_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_method_configs_is_active",
                table: "payment_method_configs",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_payment_method_configs_name",
                table: "payment_method_configs",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_method_configs_type",
                table: "payment_method_configs",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_payments");

            migrationBuilder.DropTable(
                name: "payment_method_configs");
        }
    }
}

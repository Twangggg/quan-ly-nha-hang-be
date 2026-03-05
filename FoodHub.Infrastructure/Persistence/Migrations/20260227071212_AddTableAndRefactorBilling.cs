using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class AddTableAndRefactorBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_orders_merged_into_order_id",
                table: "orders"
            );

            migrationBuilder.DropForeignKey(
                name: "fk_orders_orders_parent_order_id",
                table: "orders"
            );

            migrationBuilder.DropTable(name: "payments");

            migrationBuilder.DropTable(name: "invoices");

            migrationBuilder.DropIndex(name: "ix_orders_merged_into_order_id", table: "orders");

            migrationBuilder.DropIndex(name: "ix_orders_parent_order_id", table: "orders");

            migrationBuilder.DropColumn(name: "merged_into_order_id", table: "orders");

            migrationBuilder.DropColumn(name: "parent_order_id", table: "orders");

            migrationBuilder.DropColumn(name: "transaction_id", table: "orders");

            migrationBuilder.AddColumn<decimal>(
                name: "amount_paid",
                table: "orders",
                type: "numeric",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "payment_method",
                table: "orders",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddForeignKey(
                name: "fk_orders_tables_table_id",
                table: "orders",
                column: "table_id",
                principalTable: "tables",
                principalColumn: "table_id",
                onDelete: ReferentialAction.SetNull
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_orders_tables_table_id", table: "orders");

            migrationBuilder.DropColumn(name: "amount_paid", table: "orders");

            migrationBuilder.DropColumn(name: "paid_at", table: "orders");

            migrationBuilder.DropColumn(name: "payment_method", table: "orders");

            migrationBuilder.AddColumn<Guid>(
                name: "merged_into_order_id",
                table: "orders",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "parent_order_id",
                table: "orders",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "transaction_id",
                table: "orders",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    invoice_code = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    row_version = table.Column<byte[]>(
                        type: "bytea",
                        rowVersion: true,
                        nullable: false
                    ),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.invoice_id);
                    table.ForeignKey(
                        name: "fk_invoices_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    transaction_reference = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: true
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.payment_id);
                    table.ForeignKey(
                        name: "fk_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_orders_merged_into_order_id",
                table: "orders",
                column: "merged_into_order_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_orders_parent_order_id",
                table: "orders",
                column: "parent_order_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_code",
                table: "invoices",
                column: "invoice_code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "invoices",
                column: "order_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_payments_invoice_id",
                table: "payments",
                column: "invoice_id"
            );

            migrationBuilder.AddForeignKey(
                name: "fk_orders_orders_merged_into_order_id",
                table: "orders",
                column: "merged_into_order_id",
                principalTable: "orders",
                principalColumn: "order_id",
                onDelete: ReferentialAction.SetNull
            );

            migrationBuilder.AddForeignKey(
                name: "fk_orders_orders_parent_order_id",
                table: "orders",
                column: "parent_order_id",
                principalTable: "orders",
                principalColumn: "order_id",
                onDelete: ReferentialAction.SetNull
            );
        }
    }
}

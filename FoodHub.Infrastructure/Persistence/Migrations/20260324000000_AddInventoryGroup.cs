using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    public partial class AddInventoryGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_groups",
                columns: table => new
                {
                    inventory_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    low_stock_threshold = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    expiry_warning_days = table.Column<int>(type: "integer", nullable: true),
                    default_cost_method = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_groups", x => x.inventory_group_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_groups_name",
                table: "inventory_groups",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_group_id",
                table: "ingredients",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_inventory_group_id",
                table: "ingredients",
                column: "inventory_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredients_inventory_groups_inventory_group_id",
                table: "ingredients",
                column: "inventory_group_id",
                principalTable: "inventory_groups",
                principalColumn: "inventory_group_id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ingredients_inventory_groups_inventory_group_id",
                table: "ingredients");

            migrationBuilder.DropIndex(
                name: "ix_ingredients_inventory_group_id",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "inventory_group_id",
                table: "ingredients");

            migrationBuilder.DropTable(
                name: "inventory_groups");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryGroupProperly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE ingredients ADD COLUMN IF NOT EXISTS inventory_group_id uuid;");
            migrationBuilder.Sql("ALTER TABLE ingredients ADD COLUMN IF NOT EXISTS use_default_low_stock_threshold boolean NOT NULL DEFAULT false;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS inventory_groups (
                    inventory_group_id uuid NOT NULL,
                    name character varying(100) NOT NULL,
                    description character varying(500) NULL,
                    low_stock_threshold numeric(18,2) NULL,
                    expiry_warning_days integer NULL,
                    default_cost_method integer NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by uuid NULL,
                    updated_at timestamp with time zone NULL,
                    updated_by uuid NULL,
                    deleted_at timestamp with time zone NULL,
                    CONSTRAINT pk_inventory_groups PRIMARY KEY (inventory_group_id)
                );
            ");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_ingredients_inventory_group_id ON ingredients (inventory_group_id);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ix_inventory_groups_name ON inventory_groups (name) WHERE deleted_at IS NULL;");

            migrationBuilder.Sql(@"
                DO $$ BEGIN IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_ingredients_inventory_groups_inventory_group_id'
                ) THEN ALTER TABLE ingredients ADD CONSTRAINT fk_ingredients_inventory_groups_inventory_group_id 
                    FOREIGN KEY (inventory_group_id) REFERENCES inventory_groups(inventory_group_id) ON DELETE SET NULL;
                END IF; END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ingredients_inventory_groups_inventory_group_id",
                table: "ingredients");

            migrationBuilder.DropTable(
                name: "inventory_groups");

            migrationBuilder.DropIndex(
                name: "ix_ingredients_inventory_group_id",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "inventory_group_id",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "use_default_low_stock_threshold",
                table: "ingredients");
        }
    }
}

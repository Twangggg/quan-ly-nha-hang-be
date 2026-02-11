using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_menu_items_category_id",
                table: "menu_items");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_created_at",
                table: "set_menus",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_is_out_of_stock",
                table: "set_menus",
                column: "is_out_of_stock",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_price",
                table: "set_menus",
                column: "price");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_set_type",
                table: "set_menus",
                column: "set_type");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_set_type_is_out_of_stock",
                table: "set_menus",
                columns: new[] { "set_type", "is_out_of_stock" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_is_priority",
                table: "orders",
                column: "is_priority");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_type",
                table: "orders",
                column: "order_type");

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_table_id_status",
                table: "orders",
                columns: new[] { "table_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_items_menu_item_id",
                table: "order_items",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id_status",
                table: "order_items",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_items_status",
                table: "order_items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_status_menu_item_id",
                table: "order_items",
                columns: new[] { "status", "menu_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_option_items_extra_price",
                table: "option_items",
                column: "extra_price");

            migrationBuilder.CreateIndex(
                name: "ix_option_groups_is_required",
                table: "option_groups",
                column: "is_required");

            migrationBuilder.CreateIndex(
                name: "ix_option_groups_menu_item_id_type",
                table: "option_groups",
                columns: new[] { "menu_item_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_option_groups_type",
                table: "option_groups",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "idx_menu_items_category_id",
                table: "menu_items",
                column: "category_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_category_id_is_out_of_stock",
                table: "menu_items",
                columns: new[] { "category_id", "is_out_of_stock" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_created_at",
                table: "menu_items",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_is_out_of_stock",
                table: "menu_items",
                column: "is_out_of_stock");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_is_out_of_stock_category_id",
                table: "menu_items",
                columns: new[] { "is_out_of_stock", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_name",
                table: "menu_items",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_price_dine_in",
                table: "menu_items",
                column: "price_dine_in");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_station",
                table: "menu_items",
                column: "station");

            migrationBuilder.CreateIndex(
                name: "ix_employees_created_at",
                table: "employees",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_employees_role",
                table: "employees",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_employees_role_status",
                table: "employees",
                columns: new[] { "role", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_employees_status",
                table: "employees",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employees_status_role",
                table: "employees",
                columns: new[] { "status", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_is_active",
                table: "categories",
                column: "is_active",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_categories_type",
                table: "categories",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_categories_type_is_active",
                table: "categories",
                columns: new[] { "type", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_set_menus_created_at",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_set_menus_is_out_of_stock",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_set_menus_price",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_set_menus_set_type",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_set_menus_set_type_is_out_of_stock",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_orders_is_priority",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_order_type",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_table_id_status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_order_items_menu_item_id",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "ix_order_items_order_id_status",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "ix_order_items_status",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "ix_order_items_status_menu_item_id",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "ix_option_items_extra_price",
                table: "option_items");

            migrationBuilder.DropIndex(
                name: "ix_option_groups_is_required",
                table: "option_groups");

            migrationBuilder.DropIndex(
                name: "ix_option_groups_menu_item_id_type",
                table: "option_groups");

            migrationBuilder.DropIndex(
                name: "ix_option_groups_type",
                table: "option_groups");

            migrationBuilder.DropIndex(
                name: "idx_menu_items_category_id",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_category_id_is_out_of_stock",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_created_at",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_is_out_of_stock",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_is_out_of_stock_category_id",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_name",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_price_dine_in",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_station",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_employees_created_at",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_role",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_role_status",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_status",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_status_role",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_categories_is_active",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_categories_type",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_categories_type_is_active",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "idx_menu_items_category_id",
                table: "menu_items",
                column: "category_id");
        }
    }
}

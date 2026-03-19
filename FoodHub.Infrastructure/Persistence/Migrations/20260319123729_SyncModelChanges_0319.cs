using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges_0319 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_option_groups_menu_item_id",
                table: "option_groups");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_item_id",
                table: "option_groups",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "menu_item_option_groups",
                columns: table => new
                {
                    menu_item_option_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    min_select = table.Column<int>(type: "integer", nullable: false),
                    max_select = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item_option_groups", x => x.menu_item_option_group_id);
                    table.ForeignKey(
                        name: "fk_menu_item_option_groups_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "menu_item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_menu_item_option_groups_option_group_id",
                        column: x => x.option_group_id,
                        principalTable: "option_groups",
                        principalColumn: "option_group_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_menu_item_option_groups_menu_item_id",
                table: "menu_item_option_groups",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_menu_item_option_groups_option_group_id",
                table: "menu_item_option_groups",
                column: "option_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_option_groups_menu_item_id_option_group_id",
                table: "menu_item_option_groups",
                columns: new[] { "menu_item_id", "option_group_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_option_groups_menu_item_id",
                table: "option_groups",
                column: "menu_item_id",
                principalTable: "menu_items",
                principalColumn: "menu_item_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                INSERT INTO menu_item_option_groups
                (
                    menu_item_option_group_id,
                    menu_item_id,
                    option_group_id,
                    is_required,
                    min_select,
                    max_select,
                    sort_order,
                    is_visible,
                    created_at,
                    updated_at,
                    deleted_at,
                    created_by,
                    updated_by
                )
                SELECT
                    (
                        substr(md5(og.option_group_id::text || og.menu_item_id::text), 1, 8) || '-' ||
                        substr(md5(og.option_group_id::text || og.menu_item_id::text), 9, 4) || '-' ||
                        substr(md5(og.option_group_id::text || og.menu_item_id::text), 13, 4) || '-' ||
                        substr(md5(og.option_group_id::text || og.menu_item_id::text), 17, 4) || '-' ||
                        substr(md5(og.option_group_id::text || og.menu_item_id::text), 21, 12)
                    )::uuid,
                    og.menu_item_id,
                    og.option_group_id,
                    og.is_required,
                    CASE WHEN og.is_required THEN 1 ELSE 0 END,
                    CASE WHEN og.type = 1 THEN 1 ELSE 2147483647 END,
                    0,
                    TRUE,
                    COALESCE(og.created_at, NOW()),
                    og.updated_at,
                    og.deleted_at,
                    og.created_by,
                    og.updated_by
                FROM option_groups og
                WHERE og.menu_item_id IS NOT NULL
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM menu_item_option_groups miog
                      WHERE miog.menu_item_id = og.menu_item_id
                        AND miog.option_group_id = og.option_group_id
                        AND miog.deleted_at IS NULL
                  );
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_option_groups_menu_item_id",
                table: "option_groups");

            migrationBuilder.DropTable(
                name: "menu_item_option_groups");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_item_id",
                table: "option_groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_option_groups_menu_item_id",
                table: "option_groups",
                column: "menu_item_id",
                principalTable: "menu_items",
                principalColumn: "menu_item_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_areas", x => x.area_id);
                });

            migrationBuilder.CreateTable(
                name: "tables",
                columns: table => new
                {
                    table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    shape = table.Column<int>(type: "integer", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tables", x => x.table_id);
                    table.ForeignKey(
                        name: "fk_tables_area_id",
                        column: x => x.area_id,
                        principalTable: "areas",
                        principalColumn: "area_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_areas_code_prefix",
                table: "areas",
                column: "code_prefix");

            migrationBuilder.CreateIndex(
                name: "idx_areas_created_at",
                table: "areas",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_areas_name",
                table: "areas",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_tables_area_id",
                table: "tables",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "idx_tables_created_at",
                table: "tables",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_tables_table_code",
                table: "tables",
                column: "table_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tables");

            migrationBuilder.DropTable(
                name: "areas");
        }
    }
}

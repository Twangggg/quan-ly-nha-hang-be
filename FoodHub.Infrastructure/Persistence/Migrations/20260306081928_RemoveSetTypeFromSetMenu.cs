using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSetTypeFromSetMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_set_menus_set_type",
                table: "set_menus");

            migrationBuilder.DropIndex(
                name: "ix_set_menus_set_type_is_out_of_stock",
                table: "set_menus");

            migrationBuilder.DropColumn(
                name: "set_type",
                table: "set_menus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "set_type",
                table: "set_menus",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_set_type",
                table: "set_menus",
                column: "set_type");

            migrationBuilder.CreateIndex(
                name: "ix_set_menus_set_type_is_out_of_stock",
                table: "set_menus",
                columns: new[] { "set_type", "is_out_of_stock" });
        }
    }
}

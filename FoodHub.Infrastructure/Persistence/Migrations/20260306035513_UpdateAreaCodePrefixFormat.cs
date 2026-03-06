using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAreaCodePrefixFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_areas_code_prefix",
                table: "areas");

            migrationBuilder.AlterColumn<string>(
                name: "code_prefix",
                table: "areas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.CreateIndex(
                name: "idx_areas_code_prefix",
                table: "areas",
                column: "code_prefix",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_areas_code_prefix",
                table: "areas");

            migrationBuilder.AlterColumn<string>(
                name: "code_prefix",
                table: "areas",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateIndex(
                name: "idx_areas_code_prefix",
                table: "areas",
                column: "code_prefix",
                unique: true,
                filter: "deleted_at IS NULL");
        }
    }
}

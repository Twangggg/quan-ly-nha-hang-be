using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoUrlToBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "branding_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: string.Empty
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "branding_settings"
            );
        }
    }
}

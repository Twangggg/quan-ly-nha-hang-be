using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendGeneralSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                table: "branding_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "brand_name",
                table: "branding_settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "business_registration_number",
                table: "branding_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "closing_time",
                table: "branding_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "branding_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cover_image_url",
                table: "branding_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "district",
                table: "branding_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "branding_settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "enable_delivery",
                table: "branding_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_ordering",
                table: "branding_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_reservation",
                table: "branding_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "enable_take_away",
                table: "branding_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "facebook",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "favicon_url",
                table: "branding_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "google_map_url",
                table: "branding_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hotline",
                table: "branding_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "instagram",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "legal_business_name",
                table: "branding_settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "opening_time",
                table: "branding_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "branding_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "province_city",
                table: "branding_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "qr_payment_image_url",
                table: "branding_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "restaurant_code",
                table: "branding_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "street_address",
                table: "branding_settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tax_code",
                table: "branding_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "time_format",
                table: "branding_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "vat_percentage",
                table: "branding_settings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ward",
                table: "branding_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "working_days",
                table: "branding_settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "zalo_oa",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_branding_settings_email",
                table: "branding_settings",
                column: "email",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_branding_settings_restaurant_code",
                table: "branding_settings",
                column: "restaurant_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_branding_settings_tax_code",
                table: "branding_settings",
                column: "tax_code",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_branding_settings_email",
                table: "branding_settings");

            migrationBuilder.DropIndex(
                name: "ix_branding_settings_restaurant_code",
                table: "branding_settings");

            migrationBuilder.DropIndex(
                name: "ix_branding_settings_tax_code",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "branch_code",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "brand_name",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "business_registration_number",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "closing_time",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "country",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "cover_image_url",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "district",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "email",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "enable_delivery",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "enable_ordering",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "enable_reservation",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "enable_take_away",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "facebook",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "favicon_url",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "google_map_url",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "hotline",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "instagram",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "legal_business_name",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "opening_time",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "province_city",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "qr_payment_image_url",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "restaurant_code",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "street_address",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "tax_code",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "time_format",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "vat_percentage",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "ward",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "website",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "working_days",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "zalo_oa",
                table: "branding_settings");
        }
    }
}

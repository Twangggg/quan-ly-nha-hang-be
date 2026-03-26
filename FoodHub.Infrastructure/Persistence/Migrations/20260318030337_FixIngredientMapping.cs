using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixIngredientMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'fk_ingredient_uom_conversions_ingredients_ingredient_id1'
                          AND table_name = 'ingredient_uom_conversions'
                    ) THEN
                        ALTER TABLE ingredient_uom_conversions DROP CONSTRAINT fk_ingredient_uom_conversions_ingredients_ingredient_id1;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'ix_ingredient_uom_conversions_ingredient_id1'
                          AND tablename = 'ingredient_uom_conversions'
                    ) THEN
                        DROP INDEX ix_ingredient_uom_conversions_ingredient_id1;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ingredient_uom_conversions' 
                          AND column_name = 'ingredient_id1'
                    ) THEN
                        ALTER TABLE ingredient_uom_conversions DROP COLUMN ingredient_id1;
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ingredient_id1",
                table: "ingredient_uom_conversions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_uom_conversions_ingredient_id1",
                table: "ingredient_uom_conversions",
                column: "ingredient_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_ingredient_uom_conversions_ingredients_ingredient_id1",
                table: "ingredient_uom_conversions",
                column: "ingredient_id1",
                principalTable: "ingredients",
                principalColumn: "ingredient_id");
        }
    }
}

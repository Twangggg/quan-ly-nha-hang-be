using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges_20260317 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make the migration idempotent to handle existing schemas in Docker DBs
            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ingredients'
                          AND column_name = 'unit'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ingredients'
                          AND column_name = 'base_unit'
                    ) THEN
                        ALTER TABLE public.ingredients RENAME COLUMN unit TO base_unit;
                    END IF;
                END $$;");

            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'order_items'
                          AND column_name = 'stock_deducted'
                    ) THEN
                        ALTER TABLE public.order_items
                            ADD COLUMN stock_deducted boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;");

            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'ingredient_uom_conversions'
                    ) THEN
                        CREATE TABLE public.ingredient_uom_conversions (
                            ingredient_uom_conversion_id uuid NOT NULL,
                            ingredient_id uuid NOT NULL,
                            from_unit varchar(20) NOT NULL,
                            to_unit varchar(20) NOT NULL,
                            factor numeric(18,6) NOT NULL,
                            created_at timestamptz NOT NULL,
                            created_by uuid NULL,
                            updated_at timestamptz NULL,
                            updated_by uuid NULL,
                            deleted_at timestamptz NULL,
                            CONSTRAINT pk_ingredient_uom_conversions PRIMARY KEY (ingredient_uom_conversion_id),
                            CONSTRAINT fk_ingredient_uom_conversions_ingredients_ingredient_id
                                FOREIGN KEY (ingredient_id) REFERENCES public.ingredients (ingredient_id) ON DELETE CASCADE
                        );

                        CREATE UNIQUE INDEX ix_ingredient_uom_conversions_ingredient_id_from_unit_to_unit
                            ON public.ingredient_uom_conversions (ingredient_id, from_unit, to_unit)
                            WHERE deleted_at IS NULL;
                    END IF;
                END $$;");

            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'menu_item_ingredients'
                    ) THEN
                        CREATE TABLE public.menu_item_ingredients (
                            menu_item_ingredient_id uuid NOT NULL,
                            menu_item_id uuid NOT NULL,
                            ingredient_id uuid NOT NULL,
                            quantity_per_serving numeric(18,4) NOT NULL,
                            created_at timestamptz NOT NULL,
                            created_by uuid NULL,
                            updated_at timestamptz NULL,
                            updated_by uuid NULL,
                            deleted_at timestamptz NULL,
                            CONSTRAINT pk_menu_item_ingredients PRIMARY KEY (menu_item_ingredient_id),
                            CONSTRAINT fk_menu_item_ingredients_ingredients_ingredient_id
                                FOREIGN KEY (ingredient_id) REFERENCES public.ingredients (ingredient_id) ON DELETE RESTRICT,
                            CONSTRAINT fk_menu_item_ingredients_menu_items_menu_item_id
                                FOREIGN KEY (menu_item_id) REFERENCES public.menu_items (menu_item_id) ON DELETE CASCADE
                        );

                        CREATE INDEX ix_menu_item_ingredients_ingredient_id
                            ON public.menu_item_ingredients (ingredient_id);

                        CREATE UNIQUE INDEX ix_menu_item_ingredients_menu_item_id_ingredient_id
                            ON public.menu_item_ingredients (menu_item_id, ingredient_id)
                            WHERE deleted_at IS NULL;
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'ingredient_uom_conversions'
                    ) THEN
                        DROP TABLE public.ingredient_uom_conversions;
                    END IF;
                END $$;");

            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'menu_item_ingredients'
                    ) THEN
                        DROP TABLE public.menu_item_ingredients;
                    END IF;
                END $$;");

            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'order_items'
                          AND column_name = 'stock_deducted'
                    ) THEN
                        ALTER TABLE public.order_items DROP COLUMN stock_deducted;
                    END IF;
                END $$;");

            migrationBuilder.Sql(
                @"DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ingredients'
                          AND column_name = 'base_unit'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'ingredients'
                          AND column_name = 'unit'
                    ) THEN
                        ALTER TABLE public.ingredients RENAME COLUMN base_unit TO unit;
                    END IF;
                END $$;");
        }
    }
}

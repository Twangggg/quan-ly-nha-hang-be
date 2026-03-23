using System;
using System.Collections.Generic;
using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class MenuItem : BaseEntity
    {
        public Guid MenuItemId { get; set; }
        public required string Code { get; set; }
        public int ItemNumber { get; set; }
        public required string Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }

        public Guid CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        public Station Station { get; set; }
        public int ExpectedTime { get; set; } // Minutes

        public decimal Price { get; set; }
        public decimal CostPrice { get; set; } // Internal cost

        public bool IsOutOfStock { get; set; }

        public ICollection<OptionGroup> OptionGroups { get; set; } = new List<OptionGroup>();
        public ICollection<MenuItemOptionGroup> MenuItemOptionGroups { get; set; } =
            new List<MenuItemOptionGroup>();
        public ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();
        public virtual ICollection<MenuItemIngredient> Ingredients { get; set; } =
            new List<MenuItemIngredient>();

        public record RecipeItemInput(
            Guid IngredientId,
            decimal QuantityPerServing,
            string BaseUnit
        );

        public record RecipeUpdateResult(
            IReadOnlyCollection<MenuItemIngredient> Added,
            IReadOnlyCollection<MenuItemIngredient> Updated,
            IReadOnlyCollection<MenuItemIngredient> Removed
        );

        public void UpdateCostFromIngredients(IEnumerable<MenuItemIngredient> ingredients)
        {
            decimal totalCost = 0;
            foreach (var item in ingredients)
            {
                // Ensure Ingredient is loaded to access CostPrice
                if (item.Ingredient != null)
                {
                    totalCost += item.Ingredient.CostPrice * item.QuantityPerServing;
                }
            }

            CostPrice = totalCost;
            UpdatedAt = DateTime.UtcNow;
        }

        public DomainResult<RecipeUpdateResult> UpdateRecipe(
            IEnumerable<RecipeItemInput> items,
            string? instructions,
            int prepTimeMinutes,
            Guid? actorId = null
        )
        {
            var inputList = items?.ToList() ?? new List<RecipeItemInput>();

            if (inputList.Any(i => i.QuantityPerServing <= 0))
            {
                return DomainResult<RecipeUpdateResult>.Failure(
                    "MenuItemIngredient.InvalidQuantity"
                );
            }

            if (inputList.Select(i => i.IngredientId).Distinct().Count() != inputList.Count)
            {
                return DomainResult<RecipeUpdateResult>.Failure(
                    "MenuItemIngredient.DuplicateIngredient"
                );
            }

            var removed = Ingredients
                .Where(e => inputList.All(i => i.IngredientId != e.IngredientId))
                .ToList();

            foreach (var rem in removed)
            {
                Ingredients.Remove(rem);
            }

            var added = new List<MenuItemIngredient>();
            var updated = new List<MenuItemIngredient>();

            foreach (var input in inputList)
            {
                var line = Ingredients.FirstOrDefault(x => x.IngredientId == input.IngredientId);
                if (line == null)
                {
                    line = MenuItemIngredient.Create(
                        MenuItemId,
                        input.IngredientId,
                        input.QuantityPerServing,
                        input.BaseUnit,
                        actorId
                    );
                    Ingredients.Add(line);
                    added.Add(line);
                }
                else
                {
                    var updateResult = line.Update(
                        input.QuantityPerServing,
                        input.BaseUnit,
                        actorId
                    );
                    if (!updateResult.IsSuccess)
                    {
                        return DomainResult<RecipeUpdateResult>.Failure(updateResult.ErrorCode!);
                    }

                    updated.Add(line);
                }
            }

            Description = instructions;
            ExpectedTime = prepTimeMinutes;
            UpdateCostFromIngredients(Ingredients);

            return DomainResult<RecipeUpdateResult>.Success(
                new RecipeUpdateResult(added, updated, removed)
            );
        }
    }
}

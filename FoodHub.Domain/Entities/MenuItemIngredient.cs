using FoodHub.Domain.Common;

namespace FoodHub.Domain.Entities
{
    public class MenuItemIngredient : BaseEntity
    {
        protected MenuItemIngredient() { }

        public Guid MenuItemIngredientId { get; private set; }
        public Guid MenuItemId { get; private set; }
        public Guid IngredientId { get; private set; }
        public decimal QuantityPerServing { get; private set; }
        public string BaseUnit { get; private set; } = string.Empty;

        public virtual MenuItem MenuItem { get; set; } = null!;
        public virtual Ingredient Ingredient { get; set; } = null!;

        public static MenuItemIngredient Create(
            Guid menuItemId,
            Guid ingredientId,
            decimal quantityPerServing,
            string baseUnit,
            Guid? createdBy = null
        )
        {
            return new MenuItemIngredient
            {
                MenuItemIngredientId = Guid.NewGuid(),
                MenuItemId = menuItemId,
                IngredientId = ingredientId,
                QuantityPerServing = quantityPerServing,
                BaseUnit = baseUnit,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult Update(
            decimal quantityPerServing,
            string baseUnit,
            Guid? updatedBy = null
        )
        {
            if (quantityPerServing <= 0)
            {
                return DomainResult.Failure("MenuItemIngredient.InvalidQuantity");
            }

            QuantityPerServing = quantityPerServing;
            BaseUnit = baseUnit;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }
    }
}

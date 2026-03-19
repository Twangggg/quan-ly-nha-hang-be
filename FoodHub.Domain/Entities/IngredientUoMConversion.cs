using FoodHub.Domain.Common;

namespace FoodHub.Domain.Entities
{
    public class IngredientUoMConversion : BaseEntity
    {
        protected IngredientUoMConversion() { }

        public Guid IngredientUoMConversionId { get; private set; }
        public Guid IngredientId { get; private set; }
        public string FromUnit { get; private set; } = string.Empty;
        public string ToUnit { get; private set; } = string.Empty;
        public decimal Factor { get; private set; }

        public virtual Ingredient Ingredient { get; private set; } = null!;

        public static IngredientUoMConversion Create(
            Guid ingredientId,
            string fromUnit,
            string toUnit,
            decimal factor,
            Guid? createdBy = null
        )
        {
            return new IngredientUoMConversion
            {
                IngredientUoMConversionId = Guid.NewGuid(),
                IngredientId = ingredientId,
                FromUnit = fromUnit.Trim(),
                ToUnit = toUnit.Trim(),
                Factor = factor,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult Update(
            string fromUnit,
            string toUnit,
            decimal factor,
            Guid? updatedBy = null
        )
        {
            if (factor <= 0)
            {
                return DomainResult.Failure("IngredientConversion.InvalidFactor");
            }

            FromUnit = fromUnit.Trim();
            ToUnit = toUnit.Trim();
            Factor = factor;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
            return DomainResult.Success();
        }
    }
}

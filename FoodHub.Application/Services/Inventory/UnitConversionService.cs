using FoodHub.Domain.Entities;

namespace FoodHub.Application.Services.Inventory
{
    public interface IUnitConversionService
    {
        decimal ConvertToBase(Ingredient ingredient, string fromUnit, decimal quantity);
    }

    public class UnitConversionService : IUnitConversionService
    {
        public decimal ConvertToBase(Ingredient ingredient, string fromUnit, decimal quantity)
        {
            if (string.Equals(fromUnit, ingredient.BaseUnit, StringComparison.OrdinalIgnoreCase))
            {
                return quantity;
            }

            var conversion = ingredient.Conversions.FirstOrDefault(x =>
                string.Equals(x.FromUnit, fromUnit, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ToUnit, ingredient.BaseUnit, StringComparison.OrdinalIgnoreCase)
            );

            if (conversion == null)
            {
                throw new InvalidOperationException(
                    $"No conversion from {fromUnit} to {ingredient.BaseUnit} for ingredient {ingredient.Name}"
                );
            }

            return quantity * conversion.Factor;
        }
    }
}

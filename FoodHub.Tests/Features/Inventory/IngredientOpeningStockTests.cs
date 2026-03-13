using FluentAssertions;
using FoodHub.Domain.Entities;

namespace FoodHub.Tests.Features.Inventory
{
    public class IngredientOpeningStockTests
    {
        [Fact]
        public void SetOpeningStock_Should_OverwriteStockAndCost()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 1, null);

            var result = ingredient.SetOpeningStock(25, 5);

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(25);
            ingredient.CostPrice.Should().Be(5);
        }
    }
}

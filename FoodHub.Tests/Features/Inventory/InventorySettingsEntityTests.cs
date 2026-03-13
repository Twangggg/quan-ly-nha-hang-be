using FluentAssertions;
using FoodHub.Domain.Entities;

namespace FoodHub.Tests.Features.Inventory
{
    public class InventorySettingsEntityTests
    {
        [Fact]
        public void CreateDefault_Should_ReturnExpectedDefaults()
        {
            var settings = InventorySettings.CreateDefault();

            settings.SettingsKey.Should().Be(InventorySettings.DefaultSettingsKey);
            settings.ExpiryWarningDays.Should().Be(InventorySettings.DefaultExpiryWarningDays);
            settings.DefaultLowStockThreshold.Should().Be(
                InventorySettings.DefaultLowStockThresholdValue
            );
            settings.AutoDeductOnCompleted.Should().BeTrue();
        }

        [Fact]
        public void Update_Should_ApplyNewValues()
        {
            var settings = InventorySettings.CreateDefault();

            var result = settings.Update(14, 25, false, 60);

            result.IsSuccess.Should().BeTrue();
            settings.ExpiryWarningDays.Should().Be(14);
            settings.DefaultLowStockThreshold.Should().Be(25);
            settings.AutoDeductOnCompleted.Should().BeFalse();
            settings.MaxCostRecalcDays.Should().Be(60);
        }
    }
}

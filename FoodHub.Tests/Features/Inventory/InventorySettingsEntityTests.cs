using FluentAssertions;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

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

        [Fact]
        public void CompleteOpeningStock_Should_MarkSettingsAsLocked()
        {
            var settings = InventorySettings.CreateDefault();

            var result = settings.CompleteOpeningStock();

            result.IsSuccess.Should().BeTrue();
            settings.OpeningStockStatus.Should().Be(Domain.Enums.OpeningStockStatus.Completed);
            settings.LockedAt.Should().NotBeNull();
        }
    }
}

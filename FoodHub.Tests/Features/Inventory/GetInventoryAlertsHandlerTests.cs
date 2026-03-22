using FluentAssertions;
using FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlerts;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Services;
using MockQueryable.Moq;
using Moq;
using System.Reflection;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetInventoryAlertsHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnLowStockAndExpiryBadgeSummary()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCache = new Mock<ICacheService>();
            var mockSettingsRepo = new Mock<IGenericRepository<InventorySettings>>();
            var mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            var mockLotRepo = new Mock<IGenericRepository<InventoryLot>>();

            var outOfStockIngredient = Ingredient.Create("ING001", "Pepper", "Kg", 3, 0, 2, null);
            var lowStockIngredient = Ingredient.Create("ING002", "Onion", "Kg", 5, 2, 1, null);
            var expiryIngredient = Ingredient.Create("ING003", "Beef", "Kg", 2, 5, 3, null);

            var nearExpiryLot = InventoryLot.Create(
                expiryIngredient.IngredientId,
                Guid.NewGuid(),
                "LOT-NEAR",
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(3),
                3,
                1
            ).Value!;
            var expiredLot = InventoryLot.Create(
                expiryIngredient.IngredientId,
                Guid.NewGuid(),
                "LOT-EXP",
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(-1),
                3,
                1
            ).Value!;

            typeof(InventoryLot)
                .GetProperty(
                    nameof(InventoryLot.Ingredient),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )!
                .SetValue(nearExpiryLot, expiryIngredient);
            typeof(InventoryLot)
                .GetProperty(
                    nameof(InventoryLot.Ingredient),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )!
                .SetValue(expiredLot, expiryIngredient);

            mockUnitOfWork
                .Setup(x => x.Repository<InventorySettings>())
                .Returns(mockSettingsRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(mockIngredientRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<InventoryLot>()).Returns(mockLotRepo.Object);

            mockSettingsRepo
                .Setup(x => x.Query())
                .Returns(new List<InventorySettings> { InventorySettings.CreateDefault() }.AsQueryable().BuildMock());
            mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<Ingredient> { outOfStockIngredient, lowStockIngredient, expiryIngredient }
                        .AsQueryable()
                        .BuildMock()
                );
            mockLotRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryLot> { nearExpiryLot, expiredLot }.AsQueryable().BuildMock());

            var handler = new GetInventoryAlertsHandler(
                mockUnitOfWork.Object,
                mockCache.Object,
                new InventoryAlertService(),
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryAlertsHandler>>()
            );

            var result = await handler.Handle(new GetInventoryAlertsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.OutOfStockItems.Should().ContainSingle();
            result.Data.LowStockItems.Should().ContainSingle();
            result.Data.NearExpiryLots.Should().ContainSingle();
            result.Data.ExpiredLots.Should().ContainSingle();
            result.Data.BadgeCount.Should().Be(4);
        }
    }
}

using FluentAssertions;
using FoodHub.Application.Features.Dashboard.Inventory.Queries.GetInventoryDashboardOverview;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Services;
using MockQueryable.Moq;
using Moq;
using System.Reflection;

namespace FoodHub.Tests.Features.Dashboard
{
    public class GetInventoryDashboardOverviewHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnInventoryDashboardSummary()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockSettingsRepo = new Mock<IGenericRepository<InventorySettings>>();
            var mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            var mockLotRepo = new Mock<IGenericRepository<InventoryLot>>();
            var mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();

            var outOfStockIngredient = Ingredient.Create("ING001", "Pepper", "Kg", 3, 0, 2, null);
            var lowStockIngredient = Ingredient.Create("ING002", "Onion", "Kg", 5, 2, 1, null);
            var normalIngredient = Ingredient.Create("ING003", "Beef", "Kg", 2, 10, 4, null);

            var nearExpiryLot = InventoryLot.Create(
                normalIngredient.IngredientId,
                Guid.NewGuid(),
                "LOT-NEAR",
                DateTime.UtcNow.AddDays(-2),
                DateTime.UtcNow.AddDays(2),
                4,
                3
            ).Value!;
            var expiredLot = InventoryLot.Create(
                normalIngredient.IngredientId,
                Guid.NewGuid(),
                "LOT-EXP",
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(-1),
                4,
                1
            ).Value!;

            typeof(InventoryLot)
                .GetProperty(
                    nameof(InventoryLot.Ingredient),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )!
                .SetValue(nearExpiryLot, normalIngredient);
            typeof(InventoryLot)
                .GetProperty(
                    nameof(InventoryLot.Ingredient),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )!
                .SetValue(expiredLot, normalIngredient);

            var stockIn = InventoryTransaction.CreateStockIn(
                normalIngredient.IngredientId,
                5,
                4,
                15,
                "IN-001"
            );
            var stockOut = InventoryTransaction.CreateStockOut(
                normalIngredient.IngredientId,
                2,
                4,
                13,
                "OUT-001"
            );
            var saleDeduction = InventoryTransaction.CreateSaleDeduction(
                normalIngredient.IngredientId,
                1,
                4,
                12,
                "SALE-001"
            );

            mockUnitOfWork
                .Setup(x => x.Repository<InventorySettings>())
                .Returns(mockSettingsRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(mockIngredientRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<InventoryLot>()).Returns(mockLotRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(mockTransactionRepo.Object);

            mockSettingsRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<InventorySettings> { InventorySettings.CreateDefault() }
                        .AsQueryable()
                        .BuildMock()
                );
            mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<Ingredient> { outOfStockIngredient, lowStockIngredient, normalIngredient }
                        .AsQueryable()
                        .BuildMock()
                );
            mockLotRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryLot> { nearExpiryLot, expiredLot }.AsQueryable().BuildMock());
            mockTransactionRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<InventoryTransaction> { stockIn, stockOut, saleDeduction }
                        .AsQueryable()
                        .BuildMock()
                );

            var handler = new GetInventoryDashboardOverviewHandler(
                mockUnitOfWork.Object,
                new InventoryAlertService(),
                Mock.Of<
                    Microsoft.Extensions.Logging.ILogger<GetInventoryDashboardOverviewHandler>
                >()
            );

            var result = await handler.Handle(
                new GetInventoryDashboardOverviewQuery(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalIngredients.Should().Be(3);
            result.Data.ActiveIngredients.Should().Be(3);
            result.Data.OutOfStockCount.Should().Be(1);
            result.Data.LowStockCount.Should().Be(1);
            result.Data.ExpiredLots.Should().Be(1);
            result.Data.NearExpiryLots.Should().Be(1);
            result.Data.BadgeCount.Should().Be(4);
            result.Data.TotalStockValue.Should().Be(42);
            result.Data.StockInToday.Should().Be(5);
            result.Data.StockOutToday.Should().Be(2);
            result.Data.SaleDeductionToday.Should().Be(1);
            result.Data.TopLowStockItems.Should().HaveCount(2);
            result.Data.TopExpiringLots.Should().HaveCount(2);
        }
    }
}

using FluentAssertions;
using FoodHub.Application.Features.Inventory.Costing.Commands.RecalculateCogs;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Services;
using MockQueryable.Moq;
using Moq;
using System.Reflection;

namespace FoodHub.Tests.Features.Inventory
{
    public class RecalculateCogsHandlerTests
    {
        [Fact]
        public async Task Handle_Should_RestateStockOutCost_InSelectedPeriod()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMessageService = new Mock<IMessageService>();
            var mockCurrentUser = new Mock<ICurrentUserService>();
            var mockCache = new Mock<ICacheService>();
            var mockSettingsRepo = new Mock<IGenericRepository<InventorySettings>>();
            var mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            var mockOpeningRepo = new Mock<IGenericRepository<InventoryTransaction>>();
            var mockStockInItemRepo = new Mock<IGenericRepository<StockInReceiptItem>>();
            var mockStockOutItemRepo = new Mock<IGenericRepository<StockOutReceiptItem>>();

            var ingredient = Ingredient.Create("ING001", "Beef", "Kg", 0, 15, 3, null);
            var stockInReceipt1 = StockInReceipt.Create("NK-1", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), null);
            stockInReceipt1.AddItem(ingredient.IngredientId, 10, "Kg", 2, null, "LOT-A");
            var stockInReceipt2 = StockInReceipt.Create("NK-2", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), null);
            stockInReceipt2.AddItem(ingredient.IngredientId, 10, "Kg", 4, null, "LOT-B");
            var stockOutReceipt = StockOutReceipt.Create("XK-1", new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), "Use");
            stockOutReceipt.AddItem(ingredient.IngredientId, 5, 0, null);
            var stockOutItem = stockOutReceipt.Items.Single();
            var stockInItem1 = stockInReceipt1.Items.Single();
            var stockInItem2 = stockInReceipt2.Items.Single();

            SetNavigation(stockInItem1, nameof(StockInReceiptItem.StockInReceipt), stockInReceipt1);
            SetNavigation(stockInItem2, nameof(StockInReceiptItem.StockInReceipt), stockInReceipt2);
            SetNavigation(stockOutItem, nameof(StockOutReceiptItem.StockOutReceipt), stockOutReceipt);

            mockCurrentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid().ToString());
            mockMessageService
                .Setup(x => x.GetMessage("InventoryCogs.Completed"))
                .Returns("done");

            mockUnitOfWork
                .Setup(x => x.Repository<InventorySettings>())
                .Returns(mockSettingsRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(mockIngredientRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(mockOpeningRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<StockInReceiptItem>())
                .Returns(mockStockInItemRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<StockOutReceiptItem>())
                .Returns(mockStockOutItemRepo.Object);

            mockSettingsRepo
                .Setup(x => x.Query())
                .Returns(new List<InventorySettings> { InventorySettings.CreateDefault() }.AsQueryable().BuildMock());
            mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            mockOpeningRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryTransaction>().AsQueryable().BuildMock());
            mockStockInItemRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceiptItem> { stockInItem1, stockInItem2 }.AsQueryable().BuildMock());
            mockStockOutItemRepo
                .Setup(x => x.Query())
                .Returns(new List<StockOutReceiptItem> { stockOutItem }.AsQueryable().BuildMock());

            mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new RecalculateCogsHandler(
                mockUnitOfWork.Object,
                mockMessageService.Object,
                mockCurrentUser.Object,
                mockCache.Object,
                new InventoryCostService(),
                Mock.Of<Microsoft.Extensions.Logging.ILogger<RecalculateCogsHandler>>()
            );

            var result = await handler.Handle(
                new RecalculateCogsCommand
                {
                    FromDate = new DateOnly(2026, 3, 1),
                    ToDate = new DateOnly(2026, 3, 31),
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            stockOutItem.UnitPrice.Should().Be(3);
            stockOutItem.LineAmount.Should().Be(15);
            result.Data!.UpdatedItems.Should().Be(1);
            result.Data.TotalAdjustmentAmount.Should().Be(15);
        }

        private static void SetNavigation(object instance, string propertyName, object value)
        {
            instance
                .GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(instance, value);
        }
    }
}

using FluentAssertions;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.CreateStockOutReceipt;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Services;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class CreateStockOutReceiptHandlerTests
    {
        [Fact]
        public async Task Handle_Should_AllocateLots_ByFefo()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMessageService = new Mock<IMessageService>();
            var mockCurrentUser = new Mock<ICurrentUserService>();
            var mockCache = new Mock<ICacheService>();
            var mockAvailabilitySync = new Mock<IInventoryAvailabilitySyncService>();
            var mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            var mockLotRepo = new Mock<IGenericRepository<InventoryLot>>();
            var mockLotMovementRepo = new Mock<IGenericRepository<InventoryLotMovement>>();
            var mockAllocationRepo = new Mock<IGenericRepository<StockOutReceiptItemLotAllocation>>();
            var mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();
            var mockReceiptRepo = new Mock<IGenericRepository<StockOutReceipt>>();

            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 8, 4.25m, null);
            var lot1 = InventoryLot.Create(
                ingredient.IngredientId,
                Guid.NewGuid(),
                "LOT-01",
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(2),
                4.25m,
                3
            ).Value!;
            var lot2 = InventoryLot.Create(
                ingredient.IngredientId,
                Guid.NewGuid(),
                "LOT-02",
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(10),
                4.25m,
                5
            ).Value!;

            var capturedAllocations = new List<StockOutReceiptItemLotAllocation>();

            mockCurrentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid().ToString());
            mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(mockIngredientRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<InventoryLot>()).Returns(mockLotRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventoryLotMovement>())
                .Returns(mockLotMovementRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<StockOutReceiptItemLotAllocation>())
                .Returns(mockAllocationRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(mockTransactionRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<StockOutReceipt>()).Returns(mockReceiptRepo.Object);

            mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            mockLotRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryLot> { lot1, lot2 }.AsQueryable().BuildMock());
            mockReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockOutReceipt>().AsQueryable().BuildMock());
            mockReceiptRepo.Setup(x => x.AddAsync(It.IsAny<StockOutReceipt>())).Returns(Task.CompletedTask);
            mockLotMovementRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryLotMovement>()))
                .Returns(Task.CompletedTask);
            mockAllocationRepo
                .Setup(x => x.AddAsync(It.IsAny<StockOutReceiptItemLotAllocation>()))
                .Callback<StockOutReceiptItemLotAllocation>(capturedAllocations.Add)
                .Returns(Task.CompletedTask);
            mockTransactionRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryTransaction>()))
                .Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new CreateStockOutReceiptHandler(
                mockUnitOfWork.Object,
                mockMessageService.Object,
                mockCurrentUser.Object,
                mockCache.Object,
                new InventoryLotAllocationService(),
                mockAvailabilitySync.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CreateStockOutReceiptHandler>>()
            );

            var command = new CreateStockOutReceiptCommand
            {
                StockOutDate = DateTime.UtcNow,
                Reason = "Kitchen usage",
                Items = new List<CreateStockOutReceiptItemDto>
                {
                    new() { IngredientId = ingredient.IngredientId, Quantity = 4 },
                },
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(4);
            lot1.RemainingQuantity.Should().Be(0);
            lot2.RemainingQuantity.Should().Be(4);
            capturedAllocations.Should().HaveCount(2);
            capturedAllocations.Select(x => x.Quantity).Should().BeEquivalentTo(new[] { 3m, 1m });
        }
    }
}

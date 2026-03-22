using FluentAssertions;
using FoodHub.Application.Features.Inventory.Lots.Commands.DisposeInventoryLot;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class DisposeInventoryLotHandlerTests
    {
        [Fact]
        public async Task Handle_Should_DisposeQuantity_AndReduceIngredientStock()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMessageService = new Mock<IMessageService>();
            var mockCurrentUser = new Mock<ICurrentUserService>();
            var mockCache = new Mock<ICacheService>();
            var mockLotRepo = new Mock<IGenericRepository<InventoryLot>>();
            var mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            var mockMovementRepo = new Mock<IGenericRepository<InventoryLotMovement>>();
            var mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();

            var ingredient = Ingredient.Create("ING001", "Milk", "L", 0, 10, 2, null);
            var lot = InventoryLot.Create(
                ingredient.IngredientId,
                Guid.NewGuid(),
                "LOT-01",
                DateTime.UtcNow.AddDays(-3),
                DateTime.UtcNow.AddDays(1),
                2,
                10
            ).Value!;

            mockCurrentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid().ToString());
            mockUnitOfWork.Setup(x => x.Repository<InventoryLot>()).Returns(mockLotRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(mockIngredientRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventoryLotMovement>())
                .Returns(mockMovementRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(mockTransactionRepo.Object);

            mockLotRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryLot> { lot }.AsQueryable().BuildMock());
            mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            mockMovementRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryLotMovement>()))
                .Returns(Task.CompletedTask);
            mockTransactionRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryTransaction>()))
                .Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new DisposeInventoryLotHandler(
                mockUnitOfWork.Object,
                mockMessageService.Object,
                mockCurrentUser.Object,
                mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<DisposeInventoryLotHandler>>()
            );

            var result = await handler.Handle(
                new DisposeInventoryLotCommand
                {
                    LotId = lot.InventoryLotId,
                    Quantity = 2,
                    Reason = "Expired portion",
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(8);
            lot.RemainingQuantity.Should().Be(8);
            result.Data!.RemainingQuantity.Should().Be(8);
        }
    }
}

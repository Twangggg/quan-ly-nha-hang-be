using FluentAssertions;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Inventory.StockInReceipts.Commands.ReverseStockInReceipt;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class ReverseStockInReceiptHandlerTests
    {
        private readonly Mock<IInventoryAvailabilitySyncService> _availabilitySyncService;
        private readonly ReverseStockInReceiptHandler _handler;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IGenericRepository<Ingredient>> _mockIngredientRepo;
        private readonly Mock<IGenericRepository<InventoryTransaction>> _mockTransactionRepo;
        private readonly Mock<IGenericRepository<StockInReceipt>> _mockReceiptRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public ReverseStockInReceiptHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _availabilitySyncService = new Mock<IInventoryAvailabilitySyncService>();
            _mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();
            _mockReceiptRepo = new Mock<IGenericRepository<StockInReceipt>>();

            _mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(_mockIngredientRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(_mockTransactionRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<StockInReceipt>())
                .Returns(_mockReceiptRepo.Object);

            _handler = new ReverseStockInReceiptHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object,
                _availabilitySyncService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<ReverseStockInReceiptHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_ReverseReceipt_AndRestoreStock()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            ingredient.ReceiveStock(5, 6);

            var receipt = StockInReceipt.Create("NK-20260315-0001", DateTime.UtcNow, null);
            receipt.AddItem(ingredient.IngredientId, 5, "Kg", 6, null, "BATCH-01");

            var existingTransactions = new List<InventoryTransaction>
            {
                InventoryTransaction.CreateStockIn(
                    ingredient.IngredientId,
                    5,
                    6,
                    ingredient.CurrentStock,
                    receipt.ReceiptCode
                ),
            };

            _mockReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt> { receipt }.AsQueryable().BuildMock());
            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockTransactionRepo
                .Setup(x => x.Query())
                .Returns(existingTransactions.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(
                new ReverseStockInReceiptCommand(receipt.StockInReceiptId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(10);
            ingredient.CostPrice.Should().Be(3);
            receipt.DeletedAt.Should().NotBeNull();
            _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<InventoryTransaction>()), Times.Once);
            _availabilitySyncService.Verify(
                x => x.SyncAfterStockChangeAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(ingredient.IngredientId)),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_WhenReceiptIsNotLatestMovement()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            ingredient.ReceiveStock(5, 6);

            var receipt = StockInReceipt.Create("NK-20260315-0001", DateTime.UtcNow, null);
            receipt.AddItem(ingredient.IngredientId, 5, "Kg", 6, null, "BATCH-01");

            var existingTransactions = new List<InventoryTransaction>
            {
                InventoryTransaction.CreateStockIn(
                    ingredient.IngredientId,
                    5,
                    6,
                    ingredient.CurrentStock,
                    "NK-20260315-9999"
                ),
            };

            _mockReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt> { receipt }.AsQueryable().BuildMock());
            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockTransactionRepo
                .Setup(x => x.Query())
                .Returns(existingTransactions.AsQueryable().BuildMock());
            _mockMessageService
                .Setup(x => x.GetMessage("StockInReceipt.ReverseNotLatestMovement"))
                .Returns("not latest");

            var action = async () =>
                await _handler.Handle(
                    new ReverseStockInReceiptCommand(receipt.StockInReceiptId),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BusinessException>().WithMessage("not latest");
        }
    }
}

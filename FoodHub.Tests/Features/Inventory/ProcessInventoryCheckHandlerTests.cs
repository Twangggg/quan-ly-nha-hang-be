using FluentAssertions;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Inventory.InventoryChecks.Commands.ProcessInventoryCheck;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class ProcessInventoryCheckHandlerTests
    {
        private readonly Mock<IInventoryAvailabilitySyncService> _availabilitySyncService;
        private readonly ProcessInventoryCheckHandler _handler;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IGenericRepository<Ingredient>> _mockIngredientRepo;
        private readonly Mock<IGenericRepository<InventoryCheck>> _mockInventoryCheckRepo;
        private readonly Mock<IGenericRepository<InventoryTransaction>> _mockTransactionRepo;
        private readonly Mock<IGenericRepository<StockInReceipt>> _mockStockInReceiptRepo;
        private readonly Mock<IGenericRepository<StockOutReceipt>> _mockStockOutReceiptRepo;
        private readonly Mock<IReceiptCodeGenerator> _mockReceiptCodeGenerator;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public ProcessInventoryCheckHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _availabilitySyncService = new Mock<IInventoryAvailabilitySyncService>();
            _mockCache = new Mock<ICacheService>();
            _mockReceiptCodeGenerator = new Mock<IReceiptCodeGenerator>();
            _mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _mockInventoryCheckRepo = new Mock<IGenericRepository<InventoryCheck>>();
            _mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();
            _mockStockInReceiptRepo = new Mock<IGenericRepository<StockInReceipt>>();
            _mockStockOutReceiptRepo = new Mock<IGenericRepository<StockOutReceipt>>();

            _mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(_mockIngredientRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryCheck>())
                .Returns(_mockInventoryCheckRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(_mockTransactionRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<StockInReceipt>())
                .Returns(_mockStockInReceiptRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<StockOutReceipt>())
                .Returns(_mockStockOutReceiptRepo.Object);

            _mockReceiptCodeGenerator
                .Setup(x => x.GenerateStockInReceiptCodeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("NK-20260319-0001");
            _mockReceiptCodeGenerator
                .Setup(x => x.GenerateStockOutReceiptCodeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("XK-20260319-0001");

            _handler = new ProcessInventoryCheckHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object,
                _mockCache.Object,
                _availabilitySyncService.Object,
                _mockReceiptCodeGenerator.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<ProcessInventoryCheckHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_CreateStockInAdjustment_WhenPhysicalGreaterThanBook()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            var inventoryCheck = InventoryCheck.Create(DateTime.UtcNow);
            inventoryCheck.AddItem(ingredient.IngredientId, 10, 15, "Surplus");

            StockInReceipt? capturedReceipt = null;
            var capturedTransactions = new List<InventoryTransaction>();

            _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
            _mockInventoryCheckRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryCheck> { inventoryCheck }.AsQueryable().BuildMock());
            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockStockInReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt>().AsQueryable().BuildMock());
            _mockStockOutReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockOutReceipt>().AsQueryable().BuildMock());
            _mockStockInReceiptRepo
                .Setup(x => x.AddAsync(It.IsAny<StockInReceipt>()))
                .Callback<StockInReceipt>(receipt => capturedReceipt = receipt)
                .Returns(Task.CompletedTask);
            _mockTransactionRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryTransaction>()))
                .Callback<InventoryTransaction>(transaction => capturedTransactions.Add(transaction))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(
                new ProcessInventoryCheckCommand(inventoryCheck.InventoryCheckId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data!.Status.Should().Be(InventoryCheckStatus.Processed);
            ingredient.CurrentStock.Should().Be(15);
            capturedReceipt.Should().NotBeNull();
            capturedReceipt!.ReceiptType.Should().Be(InventoryReceiptType.InventoryAdjustment);
            capturedTransactions.Should().ContainSingle();
            capturedTransactions.Single().TransactionType.Should().Be(InventoryTransactionType.InventoryCheck);
            capturedTransactions.Single().Quantity.Should().Be(5);
            _availabilitySyncService.Verify(
                x => x.SyncAfterStockChangeAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(ingredient.IngredientId)),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_CreateStockOutAdjustment_WhenPhysicalLessThanBook()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            var inventoryCheck = InventoryCheck.Create(DateTime.UtcNow);
            inventoryCheck.AddItem(ingredient.IngredientId, 10, 7, "Deficit");

            StockOutReceipt? capturedReceipt = null;
            var capturedTransactions = new List<InventoryTransaction>();

            _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
            _mockInventoryCheckRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryCheck> { inventoryCheck }.AsQueryable().BuildMock());
            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockStockInReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt>().AsQueryable().BuildMock());
            _mockStockOutReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockOutReceipt>().AsQueryable().BuildMock());
            _mockStockOutReceiptRepo
                .Setup(x => x.AddAsync(It.IsAny<StockOutReceipt>()))
                .Callback<StockOutReceipt>(receipt => capturedReceipt = receipt)
                .Returns(Task.CompletedTask);
            _mockTransactionRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryTransaction>()))
                .Callback<InventoryTransaction>(transaction => capturedTransactions.Add(transaction))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(
                new ProcessInventoryCheckCommand(inventoryCheck.InventoryCheckId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(7);
            capturedReceipt.Should().NotBeNull();
            capturedReceipt!.ReceiptType.Should().Be(InventoryReceiptType.InventoryAdjustment);
            capturedTransactions.Should().ContainSingle();
            capturedTransactions.Single().Quantity.Should().Be(-3);
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_WhenInventoryCheckAlreadyProcessed()
        {
            var inventoryCheck = InventoryCheck.Create(DateTime.UtcNow);
            inventoryCheck.AddItem(Guid.NewGuid(), 5, 5, null);
            inventoryCheck.MarkProcessed();

            _mockInventoryCheckRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryCheck> { inventoryCheck }.AsQueryable().BuildMock());
            _mockMessageService
                .Setup(x => x.GetMessage("InventoryCheck.InvalidStatus"))
                .Returns("invalid status");

            var action = async () =>
                await _handler.Handle(
                    new ProcessInventoryCheckCommand(inventoryCheck.InventoryCheckId),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BusinessException>().WithMessage("invalid status");
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_WhenInventoryCheckHasNoItems()
        {
            var inventoryCheck = InventoryCheck.Create(DateTime.UtcNow);

            _mockInventoryCheckRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryCheck> { inventoryCheck }.AsQueryable().BuildMock());
            _mockMessageService
                .Setup(x => x.GetMessage(DomainErrors.InventoryCheck.ItemsRequired))
                .Returns("items required");

            var action = async () =>
                await _handler.Handle(
                    new ProcessInventoryCheckCommand(inventoryCheck.InventoryCheckId),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BusinessException>().WithMessage("items required");
        }

        [Fact]
        public async Task Handle_Should_Rollback_WhenSaveFails()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            var inventoryCheck = InventoryCheck.Create(DateTime.UtcNow);
            inventoryCheck.AddItem(ingredient.IngredientId, 10, 15, "Surplus");

            _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
            _mockInventoryCheckRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryCheck> { inventoryCheck }.AsQueryable().BuildMock());
            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockStockInReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt>().AsQueryable().BuildMock());
            _mockStockOutReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockOutReceipt>().AsQueryable().BuildMock());
            _mockTransactionRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("save failed"));

            var action = async () =>
                await _handler.Handle(
                    new ProcessInventoryCheckCommand(inventoryCheck.InventoryCheckId),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<Exception>().WithMessage("save failed");
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}

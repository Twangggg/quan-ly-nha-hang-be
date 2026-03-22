using FluentAssertions;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class CreateStockInReceiptHandlerTests
    {
        private readonly Mock<IInventoryAvailabilitySyncService> _availabilitySyncService;
        private readonly CreateStockInReceiptHandler _handler;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IGenericRepository<Ingredient>> _mockIngredientRepo;
        private readonly Mock<IGenericRepository<InventoryLot>> _mockInventoryLotRepo;
        private readonly Mock<IGenericRepository<InventoryLotMovement>> _mockInventoryLotMovementRepo;
        private readonly Mock<IGenericRepository<InventoryTransaction>> _mockTransactionRepo;
        private readonly Mock<IGenericRepository<StockInReceipt>> _mockReceiptRepo;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public CreateStockInReceiptHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCache = new Mock<ICacheService>();
            _availabilitySyncService = new Mock<IInventoryAvailabilitySyncService>();
            _mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _mockInventoryLotRepo = new Mock<IGenericRepository<InventoryLot>>();
            _mockInventoryLotMovementRepo = new Mock<IGenericRepository<InventoryLotMovement>>();
            _mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();
            _mockReceiptRepo = new Mock<IGenericRepository<StockInReceipt>>();

            _mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(_mockIngredientRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryLot>())
                .Returns(_mockInventoryLotRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryLotMovement>())
                .Returns(_mockInventoryLotMovementRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(_mockTransactionRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<StockInReceipt>())
                .Returns(_mockReceiptRepo.Object);

            _handler = new CreateStockInReceiptHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object,
                _mockCache.Object,
                _availabilitySyncService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CreateStockInReceiptHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_CreateReceipt_UpdateStock_AndWriteTransactions()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            StockInReceipt? capturedReceipt = null;

            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt>().AsQueryable().BuildMock());
            _mockReceiptRepo
                .Setup(x => x.AddAsync(It.IsAny<StockInReceipt>()))
                .Callback<StockInReceipt>(receipt => capturedReceipt = receipt)
                .Returns(Task.CompletedTask);
            _mockInventoryLotRepo.Setup(x => x.AddAsync(It.IsAny<InventoryLot>())).Returns(Task.CompletedTask);
            _mockInventoryLotMovementRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryLotMovement>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new CreateStockInReceiptCommand
            {
                Items = new List<CreateStockInReceiptItemDto>
                {
                    new()
                    {
                        IngredientId = ingredient.IngredientId,
                        Quantity = 5,
                        UnitCost = 6,
                        BatchCode = "BATCH-01",
                    },
                },
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ReceiptCode.Should().StartWith("NK-");
            ingredient.CurrentStock.Should().Be(15);
            ingredient.CostPrice.Should().Be(4);
            capturedReceipt.Should().NotBeNull();
            capturedReceipt!.Items.Should().HaveCount(1);
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
        public async Task Handle_Should_ThrowNotFoundException_WhenIngredientMissing()
        {
            _mockIngredientRepo.Setup(x => x.Query()).Returns(new List<Ingredient>().AsQueryable().BuildMock());
            _mockReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt>().AsQueryable().BuildMock());
            _mockInventoryLotRepo.Setup(x => x.AddAsync(It.IsAny<InventoryLot>())).Returns(Task.CompletedTask);
            _mockInventoryLotMovementRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryLotMovement>()))
                .Returns(Task.CompletedTask);
            _mockMessageService
                .Setup(x => x.GetMessage("Ingredient.NotFound"))
                .Returns("ingredient not found");

            var action = async () =>
                await _handler.Handle(
                    new CreateStockInReceiptCommand
                    {
                        Items = new List<CreateStockInReceiptItemDto>
                        {
                            new() { IngredientId = Guid.NewGuid(), Quantity = 5, UnitCost = 6 },
                        },
                    },
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<NotFoundException>().WithMessage("ingredient not found");
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_WhenIngredientInactive()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);
            ingredient.Deactivate(false);

            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt>().AsQueryable().BuildMock());
            _mockInventoryLotRepo.Setup(x => x.AddAsync(It.IsAny<InventoryLot>())).Returns(Task.CompletedTask);
            _mockInventoryLotMovementRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryLotMovement>()))
                .Returns(Task.CompletedTask);
            _mockMessageService
                .Setup(x => x.GetMessage("Ingredient.Inactive"))
                .Returns("ingredient inactive");

            var action = async () =>
                await _handler.Handle(
                    new CreateStockInReceiptCommand
                    {
                        Items = new List<CreateStockInReceiptItemDto>
                        {
                            new() { IngredientId = ingredient.IngredientId, Quantity = 5, UnitCost = 6 },
                        },
                    },
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BusinessException>().WithMessage("ingredient inactive");
        }

        [Fact]
        public async Task Handle_Should_Rollback_WhenSaveFails()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 3, null);

            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockReceiptRepo
                .Setup(x => x.Query())
                .Returns(new List<StockInReceipt>().AsQueryable().BuildMock());
            _mockInventoryLotRepo.Setup(x => x.AddAsync(It.IsAny<InventoryLot>())).Returns(Task.CompletedTask);
            _mockInventoryLotMovementRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryLotMovement>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("save failed"));

            var action = async () =>
                await _handler.Handle(
                    new CreateStockInReceiptCommand
                    {
                        Items = new List<CreateStockInReceiptItemDto>
                        {
                            new() { IngredientId = ingredient.IngredientId, Quantity = 5, UnitCost = 6 },
                        },
                    },
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<Exception>().WithMessage("save failed");
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}

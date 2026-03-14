using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class ImportOpeningStockHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IGenericRepository<Ingredient>> _ingredientRepo;
        private readonly Mock<IGenericRepository<InventoryTransaction>> _transactionRepo;
        private readonly Mock<IGenericRepository<InventorySettings>> _settingsRepo;
        private readonly ImportOpeningStockHandler _handler;

        public ImportOpeningStockHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCacheService = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _ingredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _transactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();
            _settingsRepo = new Mock<IGenericRepository<InventorySettings>>();

            _mockUow.Setup(x => x.Repository<Ingredient>()).Returns(_ingredientRepo.Object);
            _mockUow.Setup(x => x.Repository<InventoryTransaction>()).Returns(_transactionRepo.Object);
            _mockUow.Setup(x => x.Repository<InventorySettings>()).Returns(_settingsRepo.Object);

            _handler = new ImportOpeningStockHandler(
                _mockUow.Object,
                _mockCacheService.Object,
                _mockMessage.Object,
                _mockCurrentUser.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<ImportOpeningStockHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_ImportOpeningStock_And_CommitTransaction()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 0, 0, null);
            var settings = InventorySettings.CreateDefault();
            var items = new List<OpeningStockItemDto>
            {
                new() { IngredientId = ingredient.IngredientId, Quantity = 5, CostPrice = 2 },
            };

            _ingredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _settingsRepo.Setup(x => x.Query()).Returns(new List<InventorySettings>().AsQueryable().BuildMock());
            _mockUow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCacheService
                .Setup(x => x.RemoveAsync(CacheKey.InventorySettings, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(
                new ImportOpeningStockCommand(items, true),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            ingredient.CurrentStock.Should().Be(5);
            ingredient.CostPrice.Should().Be(2);
            settings.OpeningStockStatus.Should().Be(OpeningStockStatus.Completed);
            settings.LockedAt.Should().NotBeNull();
            _mockUow.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _transactionRepo.Verify(x => x.AddAsync(It.IsAny<InventoryTransaction>()), Times.Once);
            _settingsRepo.Verify(x => x.AddAsync(It.IsAny<InventorySettings>()), Times.Once);
            _mockCacheService.Verify(
                x => x.RemoveAsync(CacheKey.InventorySettings, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_When_OverwriteNotConfirmed()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 1, null);
            _settingsRepo
                .Setup(x => x.Query())
                .Returns(new List<InventorySettings> { InventorySettings.CreateDefault() }.AsQueryable().BuildMock());
            _ingredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _settingsRepo.Setup(x => x.Query()).Returns(new List<InventorySettings>().AsQueryable().BuildMock());
            _mockMessage
                .Setup(x => x.GetMessage("OpeningStock.ConfirmOverwrite"))
                .Returns("confirm overwrite");

            var action = async () =>
                await _handler.Handle(
                    new ImportOpeningStockCommand(
                        new List<OpeningStockItemDto>
                        {
                            new() { IngredientId = ingredient.IngredientId, Quantity = 5, CostPrice = 2 },
                        },
                        false
                    ),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BusinessException>().WithMessage("confirm overwrite");
        }

        [Fact]
        public async Task Handle_Should_ThrowNotFoundException_When_IngredientMissing()
        {
            _settingsRepo
                .Setup(x => x.Query())
                .Returns(new List<InventorySettings> { InventorySettings.CreateDefault() }.AsQueryable().BuildMock());
            _ingredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient>().AsQueryable().BuildMock());
            _settingsRepo.Setup(x => x.Query()).Returns(new List<InventorySettings>().AsQueryable().BuildMock());
            _mockMessage
                .Setup(x => x.GetMessage("OpeningStock.IngredientNotFound"))
                .Returns("not found");

            var action = async () =>
                await _handler.Handle(
                    new ImportOpeningStockCommand(
                        new List<OpeningStockItemDto>
                        {
                            new() { IngredientId = Guid.NewGuid(), Quantity = 5, CostPrice = 2 },
                        },
                        true
                    ),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<NotFoundException>().WithMessage("not found");
        }

        [Fact]
        public async Task Handle_Should_Rollback_When_SaveFails()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 0, 0, null);
            _settingsRepo
                .Setup(x => x.Query())
                .Returns(new List<InventorySettings> { InventorySettings.CreateDefault() }.AsQueryable().BuildMock());
            _ingredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _settingsRepo.Setup(x => x.Query()).Returns(new List<InventorySettings>().AsQueryable().BuildMock());
            _mockUow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow
                .Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("save failed"));

            var action = async () =>
                await _handler.Handle(
                    new ImportOpeningStockCommand(
                        new List<OpeningStockItemDto>
                        {
                            new() { IngredientId = ingredient.IngredientId, Quantity = 5, CostPrice = 2 },
                        },
                        true
                    ),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<Exception>().WithMessage("save failed");
            _mockUow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_When_OpeningStockAlreadyLocked()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 0, 0, null);
            var settings = InventorySettings.CreateDefault();
            settings.CompleteOpeningStock();

            _ingredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _settingsRepo
                .Setup(x => x.Query())
                .Returns(new List<InventorySettings> { settings }.AsQueryable().BuildMock());
            _mockMessage
                .Setup(x => x.GetMessage("OpeningStock.AlreadyLocked"))
                .Returns("already locked");

            var action = async () =>
                await _handler.Handle(
                    new ImportOpeningStockCommand(
                        new List<OpeningStockItemDto>
                        {
                            new() { IngredientId = ingredient.IngredientId, Quantity = 5, CostPrice = 2 },
                        },
                        true
                    ),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BusinessException>().WithMessage("already locked");
            _mockUow.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }
    }
}

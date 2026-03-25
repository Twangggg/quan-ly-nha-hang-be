using System.Linq.Expressions;
using FluentAssertions;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class CreateIngredientHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<IGenericRepository<Ingredient>> _mockRepo;
        private readonly Mock<IGenericRepository<InventorySettings>> _mockSettingsRepo;
        private readonly CreateIngredientHandler _handler;

        public CreateIngredientHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockRepo = new Mock<IGenericRepository<Ingredient>>();
            _mockSettingsRepo = new Mock<IGenericRepository<InventorySettings>>();

            _mockCurrentUser.SetupGet(x => x.UserId).Returns((string?)null);
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(_mockRepo.Object);
            _mockUow.Setup(u => u.Repository<InventorySettings>()).Returns(_mockSettingsRepo.Object);
            var settings = InventorySettings.CreateDefault();
            settings.Update(
                settings.ExpiryWarningDays,
                3,
                settings.AutoDeductOnCompleted,
                settings.CostMethod,
                settings.MaxCostRecalcDays,
                settings.OpeningStockImportCooldownHours
            );

            _mockSettingsRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<InventorySettings> { settings }
                    .AsQueryable()
                    .BuildMock()
                );

            _handler = new CreateIngredientHandler(
                _mockUow.Object,
                _mockMessage.Object,
                _mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CreateIngredientHandler>>(),
                _mockCurrentUser.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_WithGeneratedCode_When_IngredientCreated()
        {
            var command = new CreateIngredientCommand(
                null,
                "Hanh tay",
                "Kg",
                5,
                false,
                null,
                "Hanh tay Da Lat"
            );

            _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(0);

            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Hanh tay");
            result.Data.Code.Should().Be("HANHTAY-1");

            _mockRepo.Verify(r => r.AddAsync(It.Is<Ingredient>(x => x.Code == "HANHTAY-1")), Times.Once);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_UseDefaultThreshold_When_FlagEnabled()
        {
            var command = new CreateIngredientCommand(
                null,
                "Ca chua",
                "Kg",
                99,
                true
            );

            _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(0);

            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.LowStockThreshold.Should().Be(3);
            _mockRepo.Verify(r => r.AddAsync(It.Is<Ingredient>(x => x.LowStockThreshold == 3)), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_UseNextGlobalSequence_When_PreviousIngredientsExist()
        {
            var command = new CreateIngredientCommand("IGNORED", "Hanh tay", "Kg", 5, false);

            _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(1);

            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Code.Should().Be("HANHTAY-2");
            _mockRepo.Verify(r => r.AddAsync(It.Is<Ingredient>(x => x.Code == "HANHTAY-2")), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NameExists()
        {
            var command = new CreateIngredientCommand(null, "Hanh tay", "Kg", 5, false);

            _mockRepo
                .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(true);

            _mockMessage
                .Setup(m => m.GetMessage("Ingredient.NameExists"))
                .Returns("Ten nguyen lieu da ton tai");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Ten nguyen lieu da ton tai");
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Ingredient>()), Times.Never);
            _mockRepo.Verify(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()), Times.Never);
        }
    }
}

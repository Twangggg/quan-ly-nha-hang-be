using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class UpdateIngredientHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly UpdateIngredientHandler _handler;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<UpdateIngredientHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;

        public UpdateIngredientHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessage = new Mock<IMessageService>();
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<UpdateIngredientHandler>>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCurrentUser.SetupGet(x => x.UserId).Returns((string?)null);

            _handler = new UpdateIngredientHandler(
                _mockUow.Object,
                _mockMessage.Object,
                _mockLogger.Object,
                _mockCurrentUser.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_IngredientDoesNotExist()
        {
            // Arrange
            var command = new UpdateIngredientCommand(Guid.NewGuid(), "ING001", "Muối", "Kg", 1, null, true);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);
            _mockMessage.Setup(m => m.GetMessage("Ingredient.NotFound")).Returns("Ingredient not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be("Ingredient not found");
        }

        [Fact]
        public async Task Handle_Should_ReturnConflict_When_NameAlreadyExists()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var ingredient = Ingredient.Create("ING001", "Muối", "Kg", 2, 0, 0);
            typeof(Ingredient).GetProperty("IngredientId")!.SetValue(ingredient, ingredientId);

            var command = new UpdateIngredientCommand(ingredientId, "ING001", "Đường", "Kg", 5, ingredient.Description, true);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            repo.SetupSequence(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(false) // First call: Check code (no duplicate)
                .ReturnsAsync(true);  // Second call: Check name (duplicate)

            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);
            _mockMessage.Setup(m => m.GetMessage("Ingredient.NameExists")).Returns("Tên nguyên liệu đã tồn tại");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
            result.Error.Should().Be("Tên nguyên liệu đã tồn tại");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateIngredient_When_RequestValid()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var ingredient = Ingredient.Create("ING002", "Hành tím", "Kg", 3, 0, 0, "Củ nhỏ");
            typeof(Ingredient).GetProperty("IngredientId")!.SetValue(ingredient, ingredientId);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            repo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(false);

            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new UpdateIngredientCommand(ingredientId, "ING003", "Hành lá", "Bó", 10, "Rau thơm", false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            ingredient.Name.Should().Be("Hành lá");
            ingredient.Unit.Should().Be("Bó");
            ingredient.LowStockThreshold.Should().Be(10);
            ingredient.IsActive.Should().BeFalse();
            ingredient.Description.Should().Be("Rau thơm");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

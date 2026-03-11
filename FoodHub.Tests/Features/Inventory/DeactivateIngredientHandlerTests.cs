using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.DeactivateIngredient;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class DeactivateIngredientHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly DeactivateIngredientHandler _handler;

        public DeactivateIngredientHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessage = new Mock<IMessageService>();

            _handler = new DeactivateIngredientHandler(_mockUow.Object, _mockMessage.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_IngredientMissing()
        {
            // Arrange
            var command = new DeactivateIngredientCommand(Guid.NewGuid());

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
        public async Task Handle_Should_ReturnFailure_When_DomainRejects()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var ingredient = Ingredient.Create("ING003", "Bột mì", "Kg", 5);
            typeof(Ingredient).GetProperty("IngredientId")!.SetValue(ingredient, ingredientId);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);

            // Simulate domain error via partial mock
            var ingredientMock = new Mock<Ingredient>();
            ingredientMock.SetupGet(i => i.IngredientId).Returns(ingredientId);
            ingredientMock.Setup(i => i.Deactivate(It.IsAny<bool>()))
                .Returns(Domain.Common.DomainResult.Failure("Ingredient.UsedInRecipe"));

            repo.Setup(r => r.Query()).Returns(new List<Ingredient> { ingredientMock.Object }.AsQueryable().BuildMock());
            _mockMessage.Setup(m => m.GetMessage("Ingredient.UsedInRecipe")).Returns("Đang được sử dụng");

            var command = new DeactivateIngredientCommand(ingredientId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Đang được sử dụng");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Deactivate_When_Valid()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var ingredient = Ingredient.Create("ING004", "Bột ngọt", "Kg", 2);
            typeof(Ingredient).GetProperty("IngredientId")!.SetValue(ingredient, ingredientId);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new DeactivateIngredientCommand(ingredientId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            ingredient.IsActive.Should().BeFalse();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

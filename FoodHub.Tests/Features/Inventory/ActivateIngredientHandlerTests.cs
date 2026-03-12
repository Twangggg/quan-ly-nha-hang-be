using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.ActivateIngredient;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class ActivateIngredientHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly ActivateIngredientHandler _handler;

        public ActivateIngredientHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessage = new Mock<IMessageService>();

            _handler = new ActivateIngredientHandler(
                _mockUow.Object,
                _mockMessage.Object,
                Mock.Of<ILogger<ActivateIngredientHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_IngredientMissing()
        {
            // Arrange
            var command = new ActivateIngredientCommand(Guid.NewGuid());

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);
            _mockMessage.Setup(m => m.GetMessage("Ingredient.NotFound"))
                .Returns("Ingredient not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be("Ingredient not found");
        }

        [Fact]
        public async Task Handle_Should_Activate_When_Valid()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var ingredient = Ingredient.Create("ING010", "Muoi", "Kg", 1, 0, 0);
            typeof(Ingredient).GetProperty("IngredientId")!.SetValue(ingredient, ingredientId);
            typeof(Ingredient).GetProperty("IsActive")!.SetValue(ingredient, false);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new ActivateIngredientCommand(ingredientId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            ingredient.IsActive.Should().BeTrue();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

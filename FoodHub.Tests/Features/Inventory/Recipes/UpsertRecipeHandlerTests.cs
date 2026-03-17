using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe;
using FoodHub.Application.Extensions;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory.Recipes
{
    public class UpsertRecipeHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<UpsertRecipeHandler>> _loggerMock;
        private readonly UpsertRecipeHandler _handler;

        public UpsertRecipeHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _messageServiceMock = new Mock<IMessageService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<UpsertRecipeHandler>>();

            _messageServiceMock.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string s) => s);

            _handler = new UpsertRecipeHandler(
                _unitOfWorkMock.Object,
                _messageServiceMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldSuccess()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var ingredientId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var items = new List<UpsertRecipeItemDto>
            {
                new UpsertRecipeItemDto { IngredientId = ingredientId, QuantityPerServing = 2, BaseUnit = "kg" }
            };

            var command = new UpsertRecipeCommand(menuItemId, items, "Test Instructions", 10);

            var menuItem = new FoodHub.Domain.Entities.MenuItem
            {
                MenuItemId = menuItemId,
                Name = "Test Product",
                Code = "TEST01",
                ImageUrl = "test.png",
                Price = 100,
                CostPrice = 0
            };
            var ingredient = FoodHub.Domain.Entities.Ingredient.Create("ING01", "Test Ingredient", "kg", 5, 100, 10, null, userId);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);
            mockMenuItemRepo.Setup(x => x.Query()).Returns(new List<MenuItem> { menuItem }.AsQueryable().BuildMock());

            var mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _unitOfWorkMock.Setup(x => x.Repository<Ingredient>()).Returns(mockIngredientRepo.Object);
            mockIngredientRepo.Setup(x => x.Query()).Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());

            var mockRecipeRepo = new Mock<IGenericRepository<MenuItemIngredient>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItemIngredient>()).Returns(mockRecipeRepo.Object);
            mockRecipeRepo.Setup(x => x.Query()).Returns(new List<MenuItemIngredient>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_MenuItemNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var items = new List<UpsertRecipeItemDto>
            {
                new UpsertRecipeItemDto { IngredientId = Guid.NewGuid(), QuantityPerServing = 1, BaseUnit = "kg" }
            };
            var command = new UpsertRecipeCommand(menuItemId, items, null, 0);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);
            mockMenuItemRepo.Setup(x => x.Query()).Returns(new List<MenuItem>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("MenuItem.NotFound", result.Error);
        }
    }
}

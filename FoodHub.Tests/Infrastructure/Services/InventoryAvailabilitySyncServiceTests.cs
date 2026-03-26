using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Infrastructure.Services.Common;
using FoodHub.Infrastructure.Services.Inventory;
using FoodHub.Infrastructure.Services.Messaging;
using FoodHub.Infrastructure.Services.Reporting;
using FoodHub.Infrastructure.Services.External;
using FoodHub.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Infrastructure.Services
{
    public class InventoryAvailabilitySyncServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<InventoryAvailabilitySyncService>> _loggerMock;
        private readonly InventoryAvailabilitySyncService _service;

        public InventoryAvailabilitySyncServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<InventoryAvailabilitySyncService>>();
            _service = new InventoryAvailabilitySyncService(
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task SyncAfterStockChangeAsync_IngredientOutOfStock_ShouldSetMenuItemOutOfStock()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var ingredient = Ingredient.Create("ING01", "Test", "kg", 5, 10, 10, null, userId);
            // Simulate 0 stock
            ingredient.ReduceStock(10, userId);

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "DISH01",
                Name = "Test Dish",
                ImageUrl = "test.png",
                IsOutOfStock = false,
                Ingredients = new List<MenuItemIngredient>(),
            };

            var recipeLine = MenuItemIngredient.Create(menuItemId, ingredientId, 1, "kg", userId);
            recipeLine.Ingredient = ingredient;
            menuItem.Ingredients.Add(recipeLine);

            var recipeRepoMock = new Mock<IGenericRepository<MenuItemIngredient>>();
            _unitOfWorkMock
                .Setup(x => x.Repository<MenuItemIngredient>())
                .Returns(recipeRepoMock.Object);
            recipeRepoMock
                .Setup(x => x.Query())
                .Returns(
                    new List<MenuItemIngredient> { recipeLine }
                        .AsQueryable()
                        .BuildMock()
                );

            var menuItemRepoMock = new Mock<IGenericRepository<MenuItem>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItem>()).Returns(menuItemRepoMock.Object);
            menuItemRepoMock
                .Setup(x => x.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            await _service.SyncAfterStockChangeAsync(
                new[] { ingredientId },
                CancellationToken.None
            );

            // Assert
            Assert.True(menuItem.IsOutOfStock);
            menuItemRepoMock.Verify(x => x.Update(menuItem), Times.Once);
            _unitOfWorkMock.Verify(
                x => x.SaveChangeAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task SyncAfterStockChangeAsync_AllIngredientsInStock_ShouldSetMenuItemAvailable()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var ingredient = Ingredient.Create("ING01", "Test", "kg", 5, 10, 10, null, userId);

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "DISH01",
                Name = "Test Dish",
                ImageUrl = "test.png",
                IsOutOfStock = true, // Currently out
                Ingredients = new List<MenuItemIngredient>(),
            };

            var recipeLine = MenuItemIngredient.Create(menuItemId, ingredientId, 1, "kg", userId);
            recipeLine.Ingredient = ingredient;
            menuItem.Ingredients.Add(recipeLine);

            var recipeRepoMock = new Mock<IGenericRepository<MenuItemIngredient>>();
            _unitOfWorkMock
                .Setup(x => x.Repository<MenuItemIngredient>())
                .Returns(recipeRepoMock.Object);
            recipeRepoMock
                .Setup(x => x.Query())
                .Returns(
                    new List<MenuItemIngredient> { recipeLine }
                        .AsQueryable()
                        .BuildMock()
                );

            var menuItemRepoMock = new Mock<IGenericRepository<MenuItem>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItem>()).Returns(menuItemRepoMock.Object);
            menuItemRepoMock
                .Setup(x => x.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            await _service.SyncAfterStockChangeAsync(
                new[] { ingredientId },
                CancellationToken.None
            );

            // Assert
            Assert.False(menuItem.IsOutOfStock);
            menuItemRepoMock.Verify(x => x.Update(menuItem), Times.Once);
            _unitOfWorkMock.Verify(
                x => x.SaveChangeAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }
    }
}

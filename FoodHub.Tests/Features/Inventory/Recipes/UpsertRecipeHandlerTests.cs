using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions;
using FoodHub.Application.Extensions;
using FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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

            _messageServiceMock
                .Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns((string s) => s);

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
                new UpsertRecipeItemDto
                {
                    IngredientId = ingredientId,
                    QuantityPerServing = 2,
                    BaseUnit = "kg",
                },
            };

            var command = new UpsertRecipeCommand(menuItemId, items, "Test Instructions", 10);

            var menuItem = new FoodHub.Domain.Entities.MenuItem
            {
                MenuItemId = menuItemId,
                Name = "Test Product",
                Code = "TEST01",
                ImageUrl = "test.png",
                Price = 100,
                CostPrice = 0,
            };
            var ingredient = FoodHub.Domain.Entities.Ingredient.Create(
                "ING01",
                "Test Ingredient",
                "kg",
                5,
                100,
                10,
                null,
                userId
            );

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);
            mockMenuItemRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            var mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _unitOfWorkMock
                .Setup(x => x.Repository<Ingredient>())
                .Returns(mockIngredientRepo.Object);
            mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<Ingredient> { ingredient }
                        .AsQueryable()
                        .BuildMock()
                );

            var mockRecipeRepo = new Mock<IGenericRepository<MenuItemIngredient>>();
            _unitOfWorkMock
                .Setup(x => x.Repository<MenuItemIngredient>())
                .Returns(mockRecipeRepo.Object);
            mockRecipeRepo
                .Setup(x => x.Query())
                .Returns(new List<MenuItemIngredient>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(
                x => x.SaveChangeAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );
        }

        [Fact]
        public async Task Handle_MenuItemNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var items = new List<UpsertRecipeItemDto>
            {
                new UpsertRecipeItemDto
                {
                    IngredientId = Guid.NewGuid(),
                    QuantityPerServing = 1,
                    BaseUnit = "kg",
                },
            };
            var command = new UpsertRecipeCommand(menuItemId, items, null, 0);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);
            mockMenuItemRepo
                .Setup(x => x.Query())
                .Returns(new List<MenuItem>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("MenuItem.NotFound", result.Error);
        }

        [Fact]
        public async Task Handle_EmptyItems_ShouldSucceedAndSetCostToZero()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingIngredientId = Guid.NewGuid();

            var items = new List<UpsertRecipeItemDto>(); // Empty items
            var command = new UpsertRecipeCommand(menuItemId, items, "Empty Recipe", 0);

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Name = "Test Product",
                Code = "TEST01",
                ImageUrl = "test.png",
                Price = 100,
                CostPrice = 50, // Existing cost
            };

            var existingIngredient = Ingredient.Create(
                "ING01",
                "Existing",
                "kg",
                5,
                25,
                10,
                null,
                userId
            );
            var existingRecipeLine = MenuItemIngredient.Create(
                menuItemId,
                existingIngredientId,
                2,
                "kg",
                userId
            );

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
            _unitOfWorkMock
                .Setup(x => x.Repository<MenuItem>())
                .Returns(new Mock<IGenericRepository<MenuItem>>().Object);
            _unitOfWorkMock
                .Setup(x => x.Repository<MenuItem>().Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            var mockRecipeRepo = new Mock<IGenericRepository<MenuItemIngredient>>();
            _unitOfWorkMock
                .Setup(x => x.Repository<MenuItemIngredient>())
                .Returns(mockRecipeRepo.Object);
            // MenuItem now includes Ingredients in the query, so we need to set them up
            menuItem.Ingredients.Add(existingRecipeLine);

            _unitOfWorkMock
                .Setup(x => x.Repository<Ingredient>())
                .Returns(new Mock<IGenericRepository<Ingredient>>().Object);
            _unitOfWorkMock
                .Setup(x => x.Repository<Ingredient>().Query())
                .Returns(new List<Ingredient>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, menuItem.CostPrice);
            mockRecipeRepo.Verify(x => x.Delete(It.IsAny<MenuItemIngredient>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }
    }
}

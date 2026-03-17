using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory.Recipes
{
    public class GetRecipeHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly GetRecipeHandler _handler;

        public GetRecipeHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _messageServiceMock = new Mock<IMessageService>();
            _handler = new GetRecipeHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ValidId_ShouldReturnRecipe()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetRecipeQuery(menuItemId);
            
            var menuItem = new FoodHub.Domain.Entities.MenuItem
            {
                MenuItemId = menuItemId,
                Name = "Test Product",
                Code = "TEST01",
                ImageUrl = "test.png",
                Price = 100,
                CostPrice = 0
            };
            var mockRepo = new Mock<IGenericRepository<MenuItem>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItem>()).Returns(mockRepo.Object);
            mockRepo.Setup(x => x.Query()).Returns(new List<MenuItem> { menuItem }.AsQueryable().BuildMock());

            var mockRecipeRepo = new Mock<IGenericRepository<MenuItemIngredient>>();
            _unitOfWorkMock.Setup(x => x.Repository<MenuItemIngredient>()).Returns(mockRecipeRepo.Object);
            mockRecipeRepo.Setup(x => x.Query()).Returns(new List<MenuItemIngredient>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(menuItemId, result.Data.MenuItemId);
        }
    }
}

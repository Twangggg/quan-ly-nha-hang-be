using Moq;
using FluentAssertions;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;

namespace FoodHub.Tests.Features.MenuItems
{
    public class GetMenuItemByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly GetMenuItemByIdHandler _handler;

        public GetMenuItemByIdHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();

            _handler = new GetMenuItemByIdHandler(
                _mockUow.Object,
                _mockMessage.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnCachedResult_When_CacheExists()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetMenuItemByIdQuery(menuItemId);
            var cachedResponse = new GetMenuItemByIdResponse
            {
                MenuItemId = menuItemId,
                Name = "Cached Phở",
                Code = "MI001"
            };

            _mockCache.Setup(c => c.GetAsync<GetMenuItemByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedResponse);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedResponse);
            _mockUow.Verify(u => u.Repository<MenuItem>(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_MenuItemExists()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var query = new GetMenuItemByIdQuery(menuItemId);

            _mockCache.Setup(c => c.GetAsync<GetMenuItemByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetMenuItemByIdResponse?)null);

            _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<GetMenuItemByIdResponse>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Phở Bò",
                ImageUrl = "https://example.com/pho.jpg",
                Description = "Phở bò truyền thống",
                CategoryId = categoryId,
                Category = new Category { CategoryId = categoryId, Name = "Món chính" },
                Station = Station.HotKitchen,
                ExpectedTime = 15,
                PriceDineIn = 50000m,
                PriceTakeAway = 45000m,
                CostPrice = 25000m,
                IsOutOfStock = false,
                CreatedAt = DateTime.UtcNow
            };

            var menuItems = new List<MenuItem> { menuItem }.AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.MenuItemId.Should().Be(menuItemId);
            result.Data.Name.Should().Be("Phở Bò");
            result.Data.CategoryName.Should().Be("Món chính");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemNotFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetMenuItemByIdQuery(menuItemId);

            _mockCache.Setup(c => c.GetAsync<GetMenuItemByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetMenuItemByIdResponse?)null);

            var menuItems = new List<MenuItem>().AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            _mockMessage.Setup(m => m.GetMessage(MessageKeys.MenuItem.NotFound, menuItemId)).Returns($"Menu item {menuItemId} not found");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be($"Menu item {menuItemId} not found");
        }
    }
}

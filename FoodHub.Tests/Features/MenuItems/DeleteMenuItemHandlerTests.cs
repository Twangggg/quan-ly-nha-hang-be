using Moq;
using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;


namespace FoodHub.Tests.Features.MenuItems
{
    public class DeleteMenuItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly DeleteMenuItemHandler _handler;

        public DeleteMenuItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();

            _handler = new DeleteMenuItemHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCurrentUser.Object,
                _mockMessage.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_SoftDeleteMenuItem_When_Exists()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new DeleteMenuItemCommand(menuItemId);

            _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Phở Bò",
                ImageUrl = "https://example.com/pho.jpg",
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

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache.Setup(c => c.RemoveByPatternAsync("menuitem:list", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var response = new DeleteMenuItemResponse
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Phở Bò",
                ImageUrl = "https://example.com/pho.jpg",
                DeletedAt = DateTime.UtcNow
            };
            _mockMapper.Setup(m => m.Map<DeleteMenuItemResponse>(It.IsAny<MenuItem>())).Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            menuItem.DeletedAt.Should().NotBeNull();
            menuItem.UpdatedBy.Should().Be(userId);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveByPatternAsync("menuitem:list", It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(string.Format(CacheKey.MenuItemById, menuItemId), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_MenuItemNotExists()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new DeleteMenuItemCommand(menuItemId);

            var menuItems = new List<MenuItem>().AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            _mockMessage.Setup(m => m.GetMessage(MessageKeys.MenuItem.NotFound)).Returns("Menu item not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be("Menu item not found");
        }
    }
}

using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.MenuItems
{
    public class UpdateMenuItemStockStatusHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly UpdateMenuItemStockStatusHandler _handler;

        public UpdateMenuItemStockStatusHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();

            _handler = new UpdateMenuItemStockStatusHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCurrentUser.Object,
                _mockMessage.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_ManagerUpdatesStockStatus()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new UpdateMenuItemStockStatusCommand(menuItemId, true);

            _mockCurrentUser.Setup(c => c.Role).Returns("Manager");
            _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MENU001",
                Name = "Test Menu Item",
                ImageUrl = "http://example.com/image.jpg",
                IsOutOfStock = false,
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var menuItems = new List<MenuItem> { menuItem }.AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache.Setup(c => c.RemoveByPatternAsync("menuitem:list", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            menuItem.IsOutOfStock.Should().BeTrue();
            menuItem.UpdatedBy.Should().Be(userId);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveByPatternAsync("menuitem:list", It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserIsNotManager()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new UpdateMenuItemStockStatusCommand(menuItemId, true);

            _mockCurrentUser.Setup(c => c.Role).Returns("Staff");

            _mockMessage.Setup(m => m.GetMessage(MessageKeys.MenuItem.UpdateStockForbidden)).Returns("Only managers can update stock status");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
            result.Error.Should().Be("Only managers can update stock status");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemNotFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new UpdateMenuItemStockStatusCommand(menuItemId, true);

            _mockCurrentUser.Setup(c => c.Role).Returns("Manager");

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
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

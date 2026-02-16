using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.MenuItems
{
    public class UpdateMenuItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly UpdateMenuItemHandler _handler;

        public UpdateMenuItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();

            _handler = new UpdateMenuItemHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCurrentUser.Object,
                _mockMessage.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_MenuItemUpdated()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new UpdateMenuItemCommand(
                menuItemId,
                "Phở Bò Đặc Biệt",
                "https://example.com/new-image.jpg",
                "Phở bò đặc biệt với nhiều thịt",
                categoryId,
                Station.HotKitchen,
                20,
                60000m,
                55000m,
                30000m
            );

            _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());
            _mockCurrentUser.Setup(c => c.Role).Returns("Manager");

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Phở Bò",
                ImageUrl = "https://example.com/old-image.jpg",
                Description = "Phở bò thường",
                CategoryId = categoryId,
                Category = new Category { CategoryId = categoryId, Name = "Món chính" },
                Station = Station.HotKitchen,
                ExpectedTime = 15,
                PriceDineIn = 50000m,
                PriceTakeAway = 45000m,
                CostPrice = 25000m,
                CreatedAt = DateTime.UtcNow
            };

            var menuItems = new List<MenuItem> { menuItem }.AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            var categories = new List<Category> { new Category { CategoryId = categoryId, Name = "Món chính" } }.AsQueryable().BuildMock();
            var categoryRepo = new Mock<IGenericRepository<Category>>();
            categoryRepo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(categoryRepo.Object);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCache.Setup(c => c.RemoveByPatternAsync("menuitem:list", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var response = new UpdateMenuItemResponse
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Phở Bò Đặc Biệt",
                ImageUrl = "https://example.com/new-image.jpg"
            };
            _mockMapper.Setup(m => m.Map<UpdateMenuItemResponse>(It.IsAny<MenuItem>())).Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            menuItem.Name.Should().Be("Phở Bò Đặc Biệt");
            menuItem.CostPrice.Should().Be(30000m);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);

        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_MenuItemNotExists()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var command = new UpdateMenuItemCommand(
                menuItemId,
                "Phở Bò",
                "image.jpg",
                null,
                categoryId,
                Station.HotKitchen,
                15,
                50000m,
                45000m,
                null
            );

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
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CategoryNotFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var command = new UpdateMenuItemCommand(
                menuItemId,
                "Phở Bò",
                "image.jpg",
                null,
                categoryId,
                Station.HotKitchen,
                15,
                50000m,
                45000m,
                null
            );

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Phở Bò",
                ImageUrl = "",
                CategoryId = Guid.NewGuid()
            };

            var menuItems = new List<MenuItem> { menuItem }.AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            var categories = new List<Category>().AsQueryable().BuildMock();
            var categoryRepo = new Mock<IGenericRepository<Category>>();
            categoryRepo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(categoryRepo.Object);

            _mockMessage.Setup(m => m.GetMessage(MessageKeys.Category.NotFound)).Returns("Category not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_NonManagerUpdatesCost()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new UpdateMenuItemCommand(
                menuItemId,
                "Phở Bò",
                "image.jpg",
                null,
                categoryId,
                Station.HotKitchen,
                15,
                50000m,
                45000m,
                30000m
            );

            _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());
            _mockCurrentUser.Setup(c => c.Role).Returns("Waiter");

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Phở Bò",
                ImageUrl = "",
                CategoryId = categoryId,
                Category = new Category { CategoryId = categoryId, Name = "Món chính" },
                Station = Station.HotKitchen
            };

            var menuItems = new List<MenuItem> { menuItem }.AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            var categories = new List<Category> { new Category { CategoryId = categoryId, Name = "Món chính" } }.AsQueryable().BuildMock();
            var categoryRepo = new Mock<IGenericRepository<Category>>();
            categoryRepo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(categoryRepo.Object);

            _mockMessage.Setup(m => m.GetMessage(MessageKeys.MenuItem.UpdateCostForbidden)).Returns("Only Manager or Cashier can update cost price");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }
    }
}

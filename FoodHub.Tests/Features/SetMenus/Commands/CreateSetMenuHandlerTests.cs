using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SetMenus.Commands
{
    public class CreateSetMenuHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateSetMenuHandler _handler;

        public CreateSetMenuHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new CreateSetMenuHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CodeAlreadyExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new CreateSetMenuCommand(
                Code: "SET001",
                Name: "Combo 1",
                SetType: SetType.Lunch,
                Price: 15.00m,
                CostPrice: 10.00m,
                Description: "Test combo",
                ImageUrl: "https://example.com/image.jpg",
                Items: new List<SetMenuItemRequest>
                {
                    new SetMenuItemRequest { MenuItemId = Guid.NewGuid(), Quantity = 1 }
                }
            );

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SetMenu, bool>>>())).ReturnsAsync(true);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.SetMenu.CodeExists)).Returns("Code already exists");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new CreateSetMenuCommand(
                Code: "SET001",
                Name: "Combo 1",
                SetType: SetType.Lunch,
                Price: 15.00m,
                CostPrice: 10.00m,
                Description: "Test combo",
                ImageUrl: "https://example.com/image.jpg",
                Items: new List<SetMenuItemRequest>
                {
                    new SetMenuItemRequest { MenuItemId = menuItemId, Quantity = 1 }
                }
            );

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SetMenu, bool>>>())).ReturnsAsync(false);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MenuItem, bool>>>())).ReturnsAsync(0);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.MenuItem.NotFound)).Returns("Menu item not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_SetMenuCreated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new CreateSetMenuCommand(
                Code: "SET001",
                Name: "Combo 1",
                SetType: SetType.Lunch,
                Price: 15.00m,
                CostPrice: 10.00m,
                Description: "Test combo",
                ImageUrl: "https://example.com/image.jpg",
                Items: new List<SetMenuItemRequest>
                {
                    new SetMenuItemRequest { MenuItemId = menuItemId, Quantity = 1 }
                }
            );

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SetMenu, bool>>>())).ReturnsAsync(false);
            mockSetMenuRepo.Setup(r => r.AddAsync(It.IsAny<SetMenu>()));
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MenuItem, bool>>>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            _mockCacheService.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Code.Should().Be("SET001");
            result.Data.Name.Should().Be("Combo 1");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(c => c.RemoveByPatternAsync("setmenu:list", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_WithCorrectProperties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new CreateSetMenuCommand(
                Code: "SET002",
                Name: "Dinner Combo",
                SetType: SetType.Dinner,
                Price: 25.00m,
                CostPrice: 18.00m,
                Description = "Premium dinner combo",
                ImageUrl = "https://example.com/dinner.jpg",
                Items: new List<SetMenuItemRequest>
                {
                    new SetMenuItemRequest { MenuItemId = menuItemId, Quantity = 2 }
                }
            );

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<SetMenu, bool>>>())).ReturnsAsync(false);
            mockSetMenuRepo.Setup(r => r.AddAsync(It.IsAny<SetMenu>()));
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MenuItem, bool>>>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            _mockCacheService.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Price.Should().Be(25.00m);
            result.Data.CostPrice.Should().Be(18.00m);
            result.Data.SetType.Should().Be(SetType.Dinner);
            result.Data.Items.Should().NotBeEmpty();
        }
    }
}

using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SetMenus.Commands
{
    public class UpdateSetMenuHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly UpdateSetMenuHandler _handler;

        public UpdateSetMenuHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new UpdateSetMenuHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserIsNotManager()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var command = new UpdateSetMenuCommand(
                SetMenuId: setMenuId,
                Name: "Updated Combo",
                SetType: SetType.Lunch,
                Price: 20.00m,
                CostPrice: 15.00m,
                Description: "Updated description",
                ImageUrl: "https://example.com/updated.jpg",
                Items: new List<UpdateSetMenuItemRequest>()
            );

            _mockCurrentUserService.Setup(s => s.Role).Returns("Staff");
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.SetMenu.UpdateForbidden)).Returns("Update forbidden");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_SetMenuNotFound()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var command = new UpdateSetMenuCommand(
                SetMenuId: setMenuId,
                Name: "Updated Combo",
                SetType: SetType.Lunch,
                Price: 20.00m,
                CostPrice: 15.00m,
                Description: "Updated description",
                ImageUrl: "https://example.com/updated.jpg",
                Items: new List<UpdateSetMenuItemRequest>()
            );

            _mockCurrentUserService.Setup(s => s.Role).Returns("Manager");

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync((SetMenu?)null);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.SetMenu.NotFound)).Returns("Set menu not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemsNotFound()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new UpdateSetMenuCommand(
                SetMenuId: setMenuId,
                Name: "Updated Combo",
                SetType: SetType.Lunch,
                Price: 20.00m,
                CostPrice: 15.00m,
                Description: "Updated description",
                ImageUrl: "https://example.com/updated.jpg",
                Items: new List<UpdateSetMenuItemRequest>
                {
                    new UpdateSetMenuItemRequest { MenuItemId = menuItemId, Quantity = 1 }
                }
            );

            var existingSetMenu = new SetMenu
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1"
            };

            _mockCurrentUserService.Setup(s => s.Role).Returns("Manager");

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync(existingSetMenu);
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
        public async Task Handle_Should_ReturnSuccess_When_SetMenuUpdated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var setMenuId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new UpdateSetMenuCommand(
                SetMenuId: setMenuId,
                Name: "Updated Combo",
                SetType: SetType.Lunch,
                Price: 20.00m,
                CostPrice: 15.00m,
                Description: "Updated description",
                ImageUrl: "https://example.com/updated.jpg",
                Items: new List<UpdateSetMenuItemRequest>
                {
                    new UpdateSetMenuItemRequest { MenuItemId = menuItemId, Quantity = 2 }
                }
            );

            var existingSetMenu = new SetMenu
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1",
                SetType = SetType.Dinner,
                Price = 15.00m,
                CostPrice = 10.00m
            };

            _mockCurrentUserService.Setup(s => s.Role).Returns("Manager");
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync(existingSetMenu);
            mockSetMenuRepo.Setup(r => r.Update(It.IsAny<SetMenu>()));
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MenuItem, bool>>>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            var mockSetMenuItemRepo = new Mock<IGenericRepository<SetMenuItem>>();
            mockSetMenuItemRepo.Setup(r => r.Query()).Returns(new List<SetMenuItem>().AsQueryable().BuildMock());
            mockSetMenuItemRepo.Setup(r => r.AddAsync(It.IsAny<SetMenuItem>()));
            _mockUow.Setup(u => u.Repository<SetMenuItem>()).Returns(mockSetMenuItemRepo.Object);

            _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCacheService.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCacheService.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("Updated Combo");
        }
    }
}

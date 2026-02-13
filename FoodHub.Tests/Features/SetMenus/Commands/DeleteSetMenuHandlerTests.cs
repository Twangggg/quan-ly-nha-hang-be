using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SetMenus.Commands
{
    public class DeleteSetMenuHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly DeleteSetMenuHandler _handler;

        public DeleteSetMenuHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new DeleteSetMenuHandler(
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
            var command = new DeleteSetMenuCommand(setMenuId);

            _mockCurrentUserService.Setup(s => s.Role).Returns("Staff");
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.SetMenu.DeleteForbidden)).Returns("Delete forbidden");

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
            var command = new DeleteSetMenuCommand(setMenuId);

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
        public async Task Handle_Should_ReturnSuccess_When_SetMenuDeleted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var setMenuId = Guid.NewGuid();
            var command = new DeleteSetMenuCommand(setMenuId);

            var existingSetMenu = new SetMenu
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1",
                SetType = SetType.Lunch,
                Price = 15.00m,
                CostPrice = 10.00m,
                Description = "Test combo",
                ImageUrl = "https://example.com/image.jpg"
            };

            _mockCurrentUserService.Setup(s => s.Role).Returns("Manager");
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync(existingSetMenu);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            var mockSetMenuItemRepo = new Mock<IGenericRepository<SetMenuItem>>();
            mockSetMenuItemRepo
                .Setup(r => r.Query())
                .Returns(new List<SetMenuItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<SetMenuItem>()).Returns(mockSetMenuItemRepo.Object);

            _mockCacheService.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCacheService.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.SetMenuId.Should().Be(setMenuId);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

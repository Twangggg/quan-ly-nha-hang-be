using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SetMenus.Commands
{
    public class UpdateSetMenuStockStatusHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly UpdateSetMenuStockStatusHandler _handler;

        public UpdateSetMenuStockStatusHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new UpdateSetMenuStockStatusHandler(
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
            var command = new UpdateSetMenuStockStatusCommand(
                SetMenuId: setMenuId,
                IsOutOfStock: true
            );

            _mockCurrentUserService.Setup(s => s.Role).Returns("Staff");
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.SetMenu.UpdateForbidden))
                .Returns("Update forbidden");

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
            var command = new UpdateSetMenuStockStatusCommand(
                SetMenuId: setMenuId,
                IsOutOfStock: true
            );

            _mockCurrentUserService.Setup(s => s.Role).Returns("Manager");

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync((SetMenu?)null);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.SetMenu.NotFound))
                .Returns("Set menu not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_StockStatusUpdated()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var command = new UpdateSetMenuStockStatusCommand(
                SetMenuId: setMenuId,
                IsOutOfStock: true
            );

            var existingSetMenu = new SetMenu
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1",
                IsOutOfStock = false,
            };

            _mockCurrentUserService.Setup(s => s.Role).Returns("Manager");

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync(existingSetMenu);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            _mockCacheService
                .Setup(c =>
                    c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
                )
                .Returns(Task.CompletedTask);
            _mockCacheService
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingSetMenu.IsOutOfStock.Should().BeTrue();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ClearCache_When_StockStatusUpdated()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var command = new UpdateSetMenuStockStatusCommand(
                SetMenuId: setMenuId,
                IsOutOfStock: false
            );

            var existingSetMenu = new SetMenu
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1",
                IsOutOfStock = true,
            };

            _mockCurrentUserService.Setup(s => s.Role).Returns("Manager");

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync(existingSetMenu);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            _mockCacheService
                .Setup(c =>
                    c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
                )
                .Returns(Task.CompletedTask);
            _mockCacheService
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync("setmenu:list", It.IsAny<CancellationToken>()),
                Times.Once
            );
            _mockCacheService.Verify(
                c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }
    }
}

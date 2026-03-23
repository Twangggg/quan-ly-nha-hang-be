using FoodHub.Application.Common.Exceptions;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Commands.CreateOptionGroup;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class CreateOptionGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCache;
        private readonly CreateOptionGroupHandler _handler;

        public CreateOptionGroupHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();
            _handler = new CreateOptionGroupHandler(
                _mockUow.Object,
                _mockCache.Object,
                NullLogger<CreateOptionGroupHandler>.Instance,
                _mockMessageService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionGroupCreated()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new CreateOptionGroupCommand(
                MenuItemId: menuItemId,
                Name: "Size",
                Type: OptionGroupType.Single,
                IsRequired: true,
                MinSelect: 1,
                MaxSelect: 1
            );

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Test Item",
                ImageUrl = "https://example.com/image.jpg"
            };
            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo
                .Setup(r => r.Query())
                .Returns(new[] { menuItem }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);
            var mockAssignmentRepo = new Mock<IGenericRepository<MenuItemOptionGroup>>();
            _mockUow
                .Setup(u => u.Repository<MenuItemOptionGroup>())
                .Returns(mockAssignmentRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("Size");
            _mockUow.Verify(
                u => u.Repository<OptionGroup>().AddAsync(It.IsAny<OptionGroup>()),
                Times.Once
            );
            _mockUow.Verify(
                u => u.Repository<MenuItemOptionGroup>().AddAsync(It.IsAny<MenuItemOptionGroup>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowNotFound_When_MenuItemNotFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new CreateOptionGroupCommand(
                MenuItemId: menuItemId,
                Name: "Size",
                Type: OptionGroupType.Single,
                IsRequired: true,
                MinSelect: 1,
                MaxSelect: 1
            );

            // Mock empty menu item - not found
            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo
                .Setup(r => r.Query())
                .Returns(Array.Empty<MenuItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);
            var mockAssignmentRepo = new Mock<IGenericRepository<MenuItemOptionGroup>>();
            _mockUow
                .Setup(u => u.Repository<MenuItemOptionGroup>())
                .Returns(mockAssignmentRepo.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            _mockUow.Verify(
                u => u.Repository<OptionGroup>().AddAsync(It.IsAny<OptionGroup>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_CreateOptionGroup_WithCorrectProperties()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new CreateOptionGroupCommand(
                MenuItemId: menuItemId,
                Name: "Toppings",
                Type: OptionGroupType.Multi,
                IsRequired: false,
                MinSelect: 0,
                MaxSelect: 3
            );

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI002",
                Name = "Test Item 2",
                ImageUrl = "https://example.com/image2.jpg"
            };
            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo
                .Setup(r => r.Query())
                .Returns(new[] { menuItem }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            OptionGroup? capturedOptionGroup = null;
            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockOptionGroupRepo
                .Setup(r => r.AddAsync(It.IsAny<OptionGroup>()))
                .Callback<OptionGroup>(og => capturedOptionGroup = og);
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);
            var mockAssignmentRepo = new Mock<IGenericRepository<MenuItemOptionGroup>>();
            _mockUow
                .Setup(u => u.Repository<MenuItemOptionGroup>())
                .Returns(mockAssignmentRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedOptionGroup.Should().NotBeNull();
            capturedOptionGroup!.Name.Should().Be("Toppings");
            capturedOptionGroup.OptionType.Should().Be(OptionGroupType.Multi);
            capturedOptionGroup.IsRequired.Should().BeFalse();
            capturedOptionGroup.MenuItemId.Should().Be(menuItemId);
        }

        [Fact]
        public async Task Handle_Should_CreateStandaloneOptionGroup_When_MenuItemIdMissing()
        {
            var command = new CreateOptionGroupCommand(
                MenuItemId: null,
                Name: "Sugar Level",
                Type: OptionGroupType.Single,
                IsRequired: false,
                MinSelect: null,
                MaxSelect: null
            );

            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);
            var mockAssignmentRepo = new Mock<IGenericRepository<MenuItemOptionGroup>>();
            _mockUow
                .Setup(u => u.Repository<MenuItemOptionGroup>())
                .Returns(mockAssignmentRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.MenuItemId.Should().BeNull();
            _mockUow.Verify(
                u => u.Repository<OptionGroup>().AddAsync(It.IsAny<OptionGroup>()),
                Times.Once
            );
            _mockUow.Verify(
                u => u.Repository<MenuItemOptionGroup>().AddAsync(It.IsAny<MenuItemOptionGroup>()),
                Times.Never
            );
        }
    }
}

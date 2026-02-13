using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Commands.CreateOptionGroup;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class CreateOptionGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly CreateOptionGroupHandler _handler;

        public CreateOptionGroupHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _handler = new CreateOptionGroupHandler(_mockUow.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionGroupCreated()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new CreateOptionGroupCommand(
                MenuItemId: menuItemId,
                Name: "Size",
                Type: (int)OptionGroupType.Single,
                IsRequired: true
            );

            var menuItem = new MenuItem 
            { 
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Test Item",
                ImageUrl = "https://example.com/image.jpg"
            };
            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo.Setup(r => r.GetByIdAsync(menuItemId)).ReturnsAsync(menuItem);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

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
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemNotFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var command = new CreateOptionGroupCommand(
                MenuItemId: menuItemId,
                Name: "Size",
                Type: (int)OptionGroupType.Single,
                IsRequired: true
            );

            // Mock empty menu item - not found
            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo.Setup(r => r.GetByIdAsync(menuItemId)).ReturnsAsync((MenuItem?)null);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);

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
                Type: (int)OptionGroupType.Multi,
                IsRequired: false
            );

            var menuItem = new MenuItem 
            { 
                MenuItemId = menuItemId,
                Code = "MI002",
                Name = "Test Item 2",
                ImageUrl = "https://example.com/image2.jpg"
            };
            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo.Setup(r => r.GetByIdAsync(menuItemId)).ReturnsAsync(menuItem);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            OptionGroup? capturedOptionGroup = null;
            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockOptionGroupRepo
                .Setup(r => r.AddAsync(It.IsAny<OptionGroup>()))
                .Callback<OptionGroup>(og => capturedOptionGroup = og);
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

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
    }
}

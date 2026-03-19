using FoodHub.Application.Common.Exceptions;
using FluentAssertions;
using FoodHub.Application.Features.Options.Commands.CreateOptionItem;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class CreateOptionItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly CreateOptionItemHandler _handler;

        public CreateOptionItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _handler = new CreateOptionItemHandler(_mockUow.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionItemCreated()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new CreateOptionItemCommand(
                OptionGroupId: optionGroupId,
                Label: "Small",
                ExtraPrice: 0
            );

            var optionGroup = OptionGroup.Create("Size", OptionGroupType.Single, true, Guid.NewGuid());
            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockOptionGroupRepo.Setup(r => r.GetByIdAsync(optionGroupId)).ReturnsAsync(optionGroup);
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);

            var mockOptionItemRepo = new Mock<IGenericRepository<OptionItem>>();
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockOptionItemRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Label.Should().Be("Small");
            result.Data.ExtraPrice.Should().Be(0);
            _mockUow.Verify(
                u => u.Repository<OptionItem>().AddAsync(It.IsAny<OptionItem>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowNotFound_When_OptionGroupNotFound()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new CreateOptionItemCommand(
                OptionGroupId: optionGroupId,
                Label: "Small",
                ExtraPrice: 0
            );

            // Mock option group not found
            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockOptionGroupRepo.Setup(r => r.GetByIdAsync(optionGroupId)).ReturnsAsync((OptionGroup?)null);
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);

            var mockOptionItemRepo = new Mock<IGenericRepository<OptionItem>>();
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockOptionItemRepo.Object);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockUow.Verify(
                u => u.Repository<OptionItem>().AddAsync(It.IsAny<OptionItem>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_CreateOptionItem_WithCorrectProperties()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new CreateOptionItemCommand(
                OptionGroupId: optionGroupId,
                Label: "Large",
                ExtraPrice: 2.50m
            );

            var optionGroup = OptionGroup.Create("Size", OptionGroupType.Single, true, Guid.NewGuid());
            var mockOptionGroupRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockOptionGroupRepo.Setup(r => r.GetByIdAsync(optionGroupId)).ReturnsAsync(optionGroup);
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockOptionGroupRepo.Object);

            OptionItem? capturedOptionItem = null;
            var mockOptionItemRepo = new Mock<IGenericRepository<OptionItem>>();
            mockOptionItemRepo
                .Setup(r => r.AddAsync(It.IsAny<OptionItem>()))
                .Callback<OptionItem>(oi => capturedOptionItem = oi);
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockOptionItemRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedOptionItem.Should().NotBeNull();
            capturedOptionItem!.Label.Should().Be("Large");
            capturedOptionItem.ExtraPrice.Should().Be(2.50m);
            capturedOptionItem.OptionGroupId.Should().Be(optionGroupId);
        }
    }
}

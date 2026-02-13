using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Commands.UpdateOptionItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class UpdateOptionItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly UpdateOptionItemHandler _handler;

        public UpdateOptionItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _handler = new UpdateOptionItemHandler(_mockUow.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionItemUpdated()
        {
            // Arrange
            var optionItemId = Guid.NewGuid();
            var command = new UpdateOptionItemCommand(
                OptionItemId: optionItemId,
                Label: "Updated Small",
                ExtraPrice: 1.50m
            );

            var existingOptionItem = new OptionItem
            {
                OptionItemId = optionItemId,
                OptionGroupId = Guid.NewGuid(),
                Label = "Small",
                ExtraPrice = 0
            };

            var mockRepo = new Mock<IGenericRepository<OptionItem>>();
            mockRepo.Setup(r => r.GetByIdAsync(optionItemId)).ReturnsAsync(existingOptionItem);
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Label.Should().Be("Updated Small");
            result.Data.ExtraPrice.Should().Be(1.50m);
            _mockUow.Verify(
                u => u.Repository<OptionItem>().Update(It.IsAny<OptionItem>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OptionItemNotFound()
        {
            // Arrange
            var optionItemId = Guid.NewGuid();
            var command = new UpdateOptionItemCommand(
                OptionItemId: optionItemId,
                Label: "Updated Small",
                ExtraPrice: 1.50m
            );

            var mockRepo = new Mock<IGenericRepository<OptionItem>>();
            mockRepo.Setup(r => r.GetByIdAsync(optionItemId)).ReturnsAsync((OptionItem?)null);
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            _mockUow.Verify(
                u => u.Repository<OptionItem>().Update(It.IsAny<OptionItem>()),
                Times.Never
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateOptionItemProperties()
        {
            // Arrange
            var optionItemId = Guid.NewGuid();
            var command = new UpdateOptionItemCommand(
                OptionItemId: optionItemId,
                Label: "Large",
                ExtraPrice: 2.00m
            );

            var existingOptionItem = new OptionItem
            {
                OptionItemId = optionItemId,
                OptionGroupId = Guid.NewGuid(),
                Label = "Small",
                ExtraPrice = 0
            };

            var mockRepo = new Mock<IGenericRepository<OptionItem>>();
            mockRepo.Setup(r => r.GetByIdAsync(optionItemId)).ReturnsAsync(existingOptionItem);
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingOptionItem.Label.Should().Be("Large");
            existingOptionItem.ExtraPrice.Should().Be(2.00m);
        }
    }
}

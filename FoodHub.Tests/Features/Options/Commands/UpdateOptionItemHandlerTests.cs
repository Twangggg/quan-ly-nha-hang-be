using FoodHub.Application.Common.Exceptions;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Commands.UpdateOptionItem;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class UpdateOptionItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCache;
        private readonly UpdateOptionItemHandler _handler;

        public UpdateOptionItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();
            _handler = new UpdateOptionItemHandler(
                _mockUow.Object,
                _mockCache.Object,
                NullLogger<UpdateOptionItemHandler>.Instance,
                _mockMessageService.Object
            );
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

            var existingOptionItem = OptionItem.Create(Guid.NewGuid(), "Small", 0);

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
        public async Task Handle_Should_ThrowNotFound_When_OptionItemNotFound()
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

            var existingOptionItem = OptionItem.Create(Guid.NewGuid(), "Small", 0);

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

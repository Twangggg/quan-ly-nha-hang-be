using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Options.Commands.DeleteOptionItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class DeleteOptionItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly DeleteOptionItemHandler _handler;

        public DeleteOptionItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _handler = new DeleteOptionItemHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionItemDeleted()
        {
            // Arrange
            var optionItemId = Guid.NewGuid();
            var command = new DeleteOptionItemCommand(optionItemId);

            var existingOptionItem = new OptionItem
            {
                OptionItemId = optionItemId,
                OptionGroupId = Guid.NewGuid(),
                Label = "Small"
            };

            var mockRepo = new Mock<IGenericRepository<OptionItem>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<OptionItem> { existingOptionItem }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.OptionItemId.Should().Be(optionItemId);
            result.Data.DeletedAt.Should().NotBeNull();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OptionItemNotFound()
        {
            // Arrange
            var optionItemId = Guid.NewGuid();
            var command = new DeleteOptionItemCommand(optionItemId);

            var mockRepo = new Mock<IGenericRepository<OptionItem>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<OptionItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.OptionItem.NotFound))
                .Returns("Option item not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_SetDeletedAt_And_UpdatedAt_When_Deleted()
        {
            // Arrange
            var optionItemId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var command = new DeleteOptionItemCommand(optionItemId);

            var existingOptionItem = new OptionItem
            {
                OptionItemId = optionItemId,
                OptionGroupId = Guid.NewGuid(),
                Label = "Small"
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var mockRepo = new Mock<IGenericRepository<OptionItem>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<OptionItem> { existingOptionItem }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingOptionItem.DeletedAt.Should().NotBeNull();
            existingOptionItem.UpdatedAt.Should().NotBeNull();
        }
    }
}

using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Options.Commands.DeleteOptionGroup;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class DeleteOptionGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly DeleteOptionGroupHandler _handler;

        public DeleteOptionGroupHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _handler = new DeleteOptionGroupHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionGroupDeleted()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new DeleteOptionGroupCommand(optionGroupId);

            var existingOptionGroup = new OptionGroup
            {
                OptionGroupId = optionGroupId,
                MenuItemId = Guid.NewGuid(),
                Name = "Size",
                OptionType = OptionGroupType.Single,
                IsRequired = true
            };

            var mockRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<OptionGroup> { existingOptionGroup }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.OptionGroupId.Should().Be(optionGroupId);
            result.Data.DeletedAt.Should().NotBeNull();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OptionGroupNotFound()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new DeleteOptionGroupCommand(optionGroupId);

            var mockRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<OptionGroup>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.OptionGroup.NotFound))
                .Returns("Option group not found");

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
            var optionGroupId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var command = new DeleteOptionGroupCommand(optionGroupId);

            var existingOptionGroup = new OptionGroup
            {
                OptionGroupId = optionGroupId,
                MenuItemId = Guid.NewGuid(),
                Name = "Size",
                OptionType = OptionGroupType.Single,
                IsRequired = true
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            OptionGroup? capturedOptionGroup = null;
            var mockRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<OptionGroup> { existingOptionGroup }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingOptionGroup.DeletedAt.Should().NotBeNull();
            existingOptionGroup.UpdatedAt.Should().NotBeNull();
        }
    }
}

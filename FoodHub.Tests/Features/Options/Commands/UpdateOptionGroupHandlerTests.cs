using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Commands.UpdateOptionGroup;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Commands
{
    public class UpdateOptionGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly UpdateOptionGroupHandler _handler;

        public UpdateOptionGroupHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _handler = new UpdateOptionGroupHandler(_mockUow.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionGroupUpdated()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new UpdateOptionGroupCommand(
                OptionGroupId: optionGroupId,
                Name: "Updated Size",
                Type: (int)OptionGroupType.Multi,
                IsRequired: false
            );

            var existingOptionGroup = new OptionGroup
            {
                OptionGroupId = optionGroupId,
                MenuItemId = Guid.NewGuid(),
                Name = "Size",
                OptionType = OptionGroupType.Single,
                IsRequired = true,
                OptionItems = new List<OptionItem>()
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
            result.Data.Name.Should().Be("Updated Size");
            result.Data.Type.Should().Be((int)OptionGroupType.Multi);
            result.Data.IsRequired.Should().BeFalse();
            _mockUow.Verify(
                u => u.Repository<OptionGroup>().Update(It.IsAny<OptionGroup>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OptionGroupNotFound()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new UpdateOptionGroupCommand(
                OptionGroupId: optionGroupId,
                Name: "Updated Size",
                Type: (int)OptionGroupType.Multi,
                IsRequired: false
            );

            var mockRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<OptionGroup>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            _mockUow.Verify(
                u => u.Repository<OptionGroup>().Update(It.IsAny<OptionGroup>()),
                Times.Never
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateOptionGroupProperties()
        {
            // Arrange
            var optionGroupId = Guid.NewGuid();
            var command = new UpdateOptionGroupCommand(
                OptionGroupId: optionGroupId,
                Name: "New Name",
                Type: (int)OptionGroupType.Scale,
                IsRequired: true
            );

            var existingOptionGroup = new OptionGroup
            {
                OptionGroupId = optionGroupId,
                MenuItemId = Guid.NewGuid(),
                Name = "Old Name",
                OptionType = OptionGroupType.Single,
                IsRequired = false,
                OptionItems = new List<OptionItem>()
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
            existingOptionGroup.Name.Should().Be("New Name");
            existingOptionGroup.OptionType.Should().Be(OptionGroupType.Scale);
            existingOptionGroup.IsRequired.Should().BeTrue();
        }
    }
}

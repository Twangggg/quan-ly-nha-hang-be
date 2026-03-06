using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Areas.Commands.UpdateAreaStatus;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Areas.Commands.UpdateAreaStatus
{
    public class UpdateAreaStatusHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ILogger<UpdateAreaStatusHandler>> _mockLogger;

        public UpdateAreaStatusHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<UpdateAreaStatusHandler>>();
        }

        private UpdateAreaStatusHandler BuildHandler() =>
            new UpdateAreaStatusHandler(
                _mockUow.Object,
                _mockCache.Object,
                _mockMessage.Object,
                _mockCurrentUser.Object,
                _mockLogger.Object
            );

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenAreaNotExists()
        {
            // Arrange
            var command = new UpdateAreaStatusCommand(Guid.NewGuid(), true);
            var repo = new Mock<IGenericRepository<Area>>();
            repo.Setup(r => r.Query()).Returns(new List<Area>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Area>()).Returns(repo.Object);
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Area.NotFound))
                .Returns("Không tìm thấy khu vực");

            // Act
            var result = await BuildHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_ShouldDeactivateArea_WhenIsActiveFalse()
        {
            // Arrange
            var areaId = Guid.NewGuid();
            var area = new Area
            {
                AreaId = areaId,
                Name = "Tầng 1",
                CodePrefix = "T1",
                Status = AreaStatus.Active,
            };
            var command = new UpdateAreaStatusCommand(areaId, false);

            var repo = new Mock<IGenericRepository<Area>>();
            repo.Setup(r => r.Query())
                .Returns(
                    new List<Area> { area }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<Area>()).Returns(repo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
            _mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid().ToString());

            // Act
            var result = await BuildHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            area.Status.Should().Be(AreaStatus.Inactive);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

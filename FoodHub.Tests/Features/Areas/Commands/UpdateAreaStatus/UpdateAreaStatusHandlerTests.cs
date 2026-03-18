using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Areas.Commands.UpdateAreaStatus;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenAreaNotExists()
        {
            var command = new UpdateAreaStatusCommand(Guid.NewGuid(), true);
            var repo = new Mock<IGenericRepository<Area>>();
            repo.Setup(r => r.Query()).Returns(new List<Area>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Area>()).Returns(repo.Object);
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Area.NotFound))
                .Returns("Area not found");

            var result = await BuildHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_ShouldDeactivateArea_WhenIsActiveFalse()
        {
            var areaId = Guid.NewGuid();
            var area = new Area
            {
                AreaId = areaId,
                Name = "Tang 1",
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

            var result = await BuildHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            area.Status.Should().Be(AreaStatus.Inactive);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAreaAlreadyInactive()
        {
            var areaId = Guid.NewGuid();
            var area = new Area
            {
                AreaId = areaId,
                Name = "Tang 1",
                CodePrefix = "T1",
                Status = AreaStatus.Inactive,
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
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Area.DeactivateForbidden))
                .Returns("Cannot deactivate area");

            var result = await BuildHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Cannot deactivate area");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private UpdateAreaStatusHandler BuildHandler() =>
            new UpdateAreaStatusHandler(
                _mockUow.Object,
                _mockCache.Object,
                _mockMessage.Object,
                _mockCurrentUser.Object,
                _mockLogger.Object
            );
    }
}

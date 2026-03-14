using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Areas.Commands.UpdateArea;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Areas.Commands.UpdateArea
{
    public class UpdateAreaHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ILogger<UpdateAreaHandler>> _mockLogger;

        public UpdateAreaHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<UpdateAreaHandler>>();
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenAreaNotExists()
        {
            // Arrange
            var command = new UpdateAreaCommand
            {
                AreaId = Guid.NewGuid(),
                Name = "New",
                CodePrefix = "T1",
                Type = AreaType.Normal,
            };
            var repo = new Mock<IGenericRepository<Area>>();
            repo.Setup(r => r.Query()).Returns(new List<Area>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Area>()).Returns(repo.Object);
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Area.NotFound))
                .Returns("Không tìm thấy khu vực");

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mapConfig = new MapperConfiguration(cfg =>
                cfg.CreateMap<Area, GetAreaByIdResponse>(), mockLoggerFactory.Object
            );
            var handler = new UpdateAreaHandler(
                _mockUow.Object,
                mapConfig.CreateMapper(),
                _mockCache.Object,
                _mockMessage.Object
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_ShouldUpdateArea_WhenAreaExists()
        {
            // Arrange
            var areaId = Guid.NewGuid();
            var area = new Area
            {
                AreaId = areaId,
                Name = "Cũ",
                CodePrefix = "C1",
            };
            var command = new UpdateAreaCommand
            {
                AreaId = areaId,
                Name = "Mới",
                CodePrefix = "T1",
                Description = "Updated",
                Type = AreaType.Normal,
            };

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

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mapConfig = new MapperConfiguration(cfg =>
                cfg.CreateMap<Area, GetAreaByIdResponse>(), mockLoggerFactory.Object
            );
            var handler = new UpdateAreaHandler(
                _mockUow.Object,
                mapConfig.CreateMapper(),
                _mockCache.Object,
                _mockMessage.Object
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Mới");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

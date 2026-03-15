using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Areas.Queries.GetAreaById
{
    public class GetAreaByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ILogger<GetAreaByIdHandler>> _mockLogger;

        public GetAreaByIdHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<GetAreaByIdHandler>>();
        }

        private GetAreaByIdHandler BuildHandler(IMapper mapper) =>
            new GetAreaByIdHandler(
                _mockUow.Object,
                mapper,
                _mockCache.Object,
                _mockMessage.Object,
                _mockLogger.Object
            );

        [Fact]
        public async Task Handle_CacheHit_ReturnsFromCache()
        {
            // Arrange
            var areaId = Guid.NewGuid();
            var query = new GetAreaByIdQuery(areaId);
            var cacheKey = string.Format(CacheKey.AreaById, areaId);
            var cachedArea = new GetAreaByIdResponse
            {
                AreaId = areaId,
                Name = "T1",
                CodePrefix = "T1",
            };

            _mockCache
                .Setup(c =>
                    c.GetAsync<GetAreaByIdResponse>(cacheKey, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(cachedArea);

            var handler = BuildHandler(Mock.Of<IMapper>());

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedArea);
            _mockUow.Verify(u => u.Repository<Area>(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenAreaNotExists()
        {
            // Arrange
            var areaId = Guid.NewGuid();
            var query = new GetAreaByIdQuery(areaId);

            _mockCache
                .Setup(c =>
                    c.GetAsync<GetAreaByIdResponse>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetAreaByIdResponse?)null);

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
            var handler = BuildHandler(mapConfig.CreateMapper());

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_CacheMiss_ShouldReturnFromDB_AndSetCache()
        {
            // Arrange
            var areaId = Guid.NewGuid();
            var area = new Area
            {
                AreaId = areaId,
                Name = "Tầng 1",
                CodePrefix = "T1",
            };
            var query = new GetAreaByIdQuery(areaId);

            _mockCache
                .Setup(c =>
                    c.GetAsync<GetAreaByIdResponse>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetAreaByIdResponse?)null);

            var repo = new Mock<IGenericRepository<Area>>();
            repo.Setup(r => r.Query())
                .Returns(
                    new List<Area> { area }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<Area>()).Returns(repo.Object);
            _mockCache.Setup(c =>
                c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetAreaByIdResponse>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()
                )
            );

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var mapConfig = new MapperConfiguration(cfg =>
                cfg.CreateMap<Area, GetAreaByIdResponse>(), mockLoggerFactory.Object
            );
            var handler = BuildHandler(mapConfig.CreateMapper());

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Name.Should().Be("Tầng 1");
            _mockCache.Verify(
                c =>
                    c.SetAsync(
                        It.IsAny<string>(),
                        It.IsAny<GetAreaByIdResponse>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}

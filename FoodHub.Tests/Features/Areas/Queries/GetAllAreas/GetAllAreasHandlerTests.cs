using System.Text.Json;
using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Areas.Queries.GetAllAreas;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Areas.Queries.GetAllAreas
{
    public class GetAllAreasHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMapper> _mockMapper;
        private GetAllAreasHandler _handler;

        public GetAllAreasHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMapper = new Mock<IMapper>();

            _handler = new GetAllAreasHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_CacheHit_ReturnsFromCache()
        {
            // Arrange
            var query = new GetAllAreasQuery();
            var cacheKey = CacheKey.AreaList;

            var cachedResult = new List<GetAllAreasResponse>
            {
                new GetAllAreasResponse
                {
                    AreaId = Guid.NewGuid(),
                    Name = "Tầng 1",
                    CodePrefix = "T1",
                },
            };

            _mockCache
                .Setup(c =>
                    c.GetAsync<List<GetAllAreasResponse>>(cacheKey, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedResult);

            // Verify DB is not called
            _mockUow.Verify(u => u.Repository<Area>(), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRequest_CacheMiss_ReturnsFromDB()
        {
            // Arrange
            var request = new GetAllAreasQuery();

            var areas = new List<Area>
            {
                new Area
                {
                    AreaId = Guid.NewGuid(),
                    Name = "Tầng 1",
                    CodePrefix = "T1",
                },
            }
                .AsQueryable()
                .BuildMock();

            var repo = new Mock<IGenericRepository<Area>>();
            repo.Setup(r => r.Query()).Returns(areas);
            _mockUow.Setup(u => u.Repository<Area>()).Returns(repo.Object);

            // Giả lập IMapper ProjectTo cho IQueryable
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg =>
                cfg.CreateMap<Area, GetAllAreasResponse>(), mockLoggerFactory.Object));

            // Cache returns null to simulate cache miss
            _mockCache
                .Setup(c =>
                    c.GetAsync<List<GetAllAreasResponse>>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((List<GetAllAreasResponse>?)null);

            var handler = new GetAllAreasHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object
            );

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data!.First().Name.Should().Be("Tầng 1");

            // Verify cache SetAsync is called
            _mockCache.Verify(
                c =>
                    c.SetAsync(
                        It.IsAny<string>(),
                        It.IsAny<List<GetAllAreasResponse>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}

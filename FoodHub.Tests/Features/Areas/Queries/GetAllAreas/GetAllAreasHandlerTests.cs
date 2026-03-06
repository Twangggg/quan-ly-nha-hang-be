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
        private readonly Mock<ILogger<GetAllAreasHandler>> _mockLogger;
        private GetAllAreasHandler _handler;

        public GetAllAreasHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<GetAllAreasHandler>>();

            _handler = new GetAllAreasHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_CacheHit_ReturnsFromCache()
        {
            // Arrange
            var query = new GetAllAreasQuery(new PaginationParams());
            var queryJson = JsonSerializer.Serialize(query.Pagination);
            var cacheKey = $"{CacheKey.AreaList}:{queryJson.GetHashCode()}";

            var items = new List<GetAllAreasResponse>
            {
                new GetAllAreasResponse
                {
                    AreaId = Guid.NewGuid(),
                    Name = "Tầng 1",
                    CodePrefix = "T1",
                },
            };
            var cachedResult = new PagedResult<GetAllAreasResponse>(items, query.Pagination, 1);

            _mockCache
                .Setup(c =>
                    c.GetAsync<PagedResult<GetAllAreasResponse>>(
                        cacheKey,
                        It.IsAny<CancellationToken>()
                    )
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
            var request = new GetAllAreasQuery(new PaginationParams());

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
            var mockMapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Area, GetAllAreasResponse>();
            });
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mockMapperConfig);

            // Cache returns null to simulate cache miss
            _mockCache
                .Setup(c =>
                    c.GetAsync<PagedResult<GetAllAreasResponse>>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((PagedResult<GetAllAreasResponse>?)null);

            _handler = new GetAllAreasHandler(
                _mockUow.Object,
                mockMapperConfig.CreateMapper(), // Dùng mapper thật để test ProjectTo.ToPagedResultAsync
                _mockCache.Object,
                _mockLogger.Object
            );

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().Name.Should().Be("Tầng 1");

            // Verify cache SetAsync is called
            _mockCache.Verify(
                c =>
                    c.SetAsync(
                        It.IsAny<string>(),
                        It.IsAny<PagedResult<GetAllAreasResponse>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}

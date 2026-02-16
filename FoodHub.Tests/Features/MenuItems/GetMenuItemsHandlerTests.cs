using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItems;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.MenuItems
{
    public class GetMenuItemsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCache;
        private readonly GetMenuItemsHandler _handler;

        public GetMenuItemsHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<ICacheService>();

            _handler = new GetMenuItemsHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnCachedResult_When_CacheExists()
        {
            // Arrange
            var pagination = new PaginationParams();
            var query = new GetMenuItemsQuery { Pagination = pagination };

            var cachedResult = new PagedResult<GetMenuItemsResponse>(
                new List<GetMenuItemsResponse> { new GetMenuItemsResponse { MenuItemId = Guid.NewGuid(), Name = "Cached Item" } },
                pagination,
                1
            );

            _mockCache.Setup(c => c.GetAsync<PagedResult<GetMenuItemsResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedResult);
            _mockUow.Verify(u => u.Repository<MenuItem>(), Times.Never);
        }

        // Note: Testing the database query path requires integration tests with real AutoMapper configuration
        // because ProjectTo requires a real IConfigurationProvider which cannot be easily mocked.
    }
}

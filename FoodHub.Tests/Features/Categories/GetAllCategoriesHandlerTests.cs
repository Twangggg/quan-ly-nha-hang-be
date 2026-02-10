using Moq;
using FluentAssertions;
using FoodHub.Application.Features.Categories.Queries.GetAllCategories;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using AutoMapper;
using MockQueryable.Moq;

namespace FoodHub.Tests.Features.Categories
{
    public class GetAllCategoriesHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCache;
        private readonly GetAllCategoriesHandler _handler;

        public GetAllCategoriesHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<ICacheService>();

            _handler = new GetAllCategoriesHandler(_mockUow.Object, _mockMapper.Object, _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnCachedCategories_When_CacheExists()
        {
            // Arrange
            var query = new GetAllCategoriesQuery();
            var cachedCategories = new List<GetCategoriesResponse>
            {
                new GetCategoriesResponse { CategoryId = Guid.NewGuid(), Name = "Cached Category" }
            };

            _mockCache.Setup(c => c.GetAsync<List<GetCategoriesResponse>>(CacheKey.CategoryList, It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedCategories);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedCategories);
            _mockUow.Verify(u => u.Repository<Category>(), Times.Never);
        }


    }
}

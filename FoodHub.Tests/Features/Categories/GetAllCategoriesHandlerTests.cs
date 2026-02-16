using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Categories.Queries.GetAllCategories;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;


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
            var pagination = new PaginationParams();
            var query = new GetAllCategoriesQuery(pagination);
            var items = new List<GetAllCategoriesResponse> { new GetAllCategoriesResponse { CategoryId = Guid.NewGuid(), Name = "Cached Category" } };
            var cachedPagedResult = new PagedResult<GetAllCategoriesResponse>(items, pagination, 1);

            _mockCache.Setup(c => c.GetAsync<PagedResult<GetAllCategoriesResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedPagedResult);
            _mockUow.Verify(u => u.Repository<Category>(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnCategoriesFromDb_When_CacheNotExists()
        {
            // Arrange
            var pagination = new PaginationParams();
            var query = new GetAllCategoriesQuery(pagination);

            _mockCache.Setup(c => c.GetAsync<PagedResult<GetAllCategoriesResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PagedResult<GetAllCategoriesResponse>)null!);

            var categories = new List<Category>
            {
                new Category { CategoryId = Guid.NewGuid(), Name = "Category 1", CategoryType = CategoryType.Normal, IsActive = true },
                new Category { CategoryId = Guid.NewGuid(), Name = "Category 2", CategoryType = CategoryType.SpecialGroup, IsActive = true }
            }.AsQueryable().BuildMock();

            var repo = new Mock<IGenericRepository<Category>>();
            repo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(repo.Object);

            var mappedItems = new List<GetAllCategoriesResponse>
            {
                new GetAllCategoriesResponse { CategoryId = categories.ElementAt(0).CategoryId, Name = "Category 1", Type = (int)CategoryType.Normal },
                new GetAllCategoriesResponse { CategoryId = categories.ElementAt(1).CategoryId, Name = "Category 2", Type = (int)CategoryType.SpecialGroup }
            };


            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg =>
                cfg.CreateMap<Category, GetAllCategoriesResponse>()));

            _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<GetAllCategoriesResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _mockUow.Verify(u => u.Repository<Category>(), Times.AtLeastOnce);
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<GetAllCategoriesResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_NoCategoriesInDb()
        {
            // Arrange
            var pagination = new PaginationParams();
            var query = new GetAllCategoriesQuery(pagination);

            _mockCache.Setup(c => c.GetAsync<PagedResult<GetAllCategoriesResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PagedResult<GetAllCategoriesResponse>)null!);

            var categories = new List<Category>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Category>>();
            repo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(repo.Object);

            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg =>
                cfg.CreateMap<Category, GetAllCategoriesResponse>()));

            _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<GetAllCategoriesResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }
    }
}

using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Categories.Queries.GetCategoryById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Moq;

namespace FoodHub.Tests.Features.Categories
{
    public class GetCategoryByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly GetCategoryByIdHandler _handler;

        public GetCategoryByIdHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();

            _handler = new GetCategoryByIdHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockMessage.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnCachedCategory_When_CacheExists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var query = new GetCategoryByIdQuery(categoryId);
            var cachedResponse = new GetCategoryByIdResponse
            {
                CategoryId = categoryId,
                Name = "Cached Category",
                Type = (int)CategoryType.Normal
            };

            _mockCache.Setup(c => c.GetAsync<GetCategoryByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedResponse);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedResponse);
            _mockUow.Verify(u => u.Repository<Category>(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnCategoryFromDb_When_CacheNotExists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var query = new GetCategoryByIdQuery(categoryId);
            var category = new Category
            {
                CategoryId = categoryId,
                Name = "Test Category",
                CategoryType = CategoryType.Normal
            };
            var mappedResponse = new GetCategoryByIdResponse
            {
                CategoryId = categoryId,
                Name = "Test Category",
                Type = (int)CategoryType.Normal
            };

            _mockCache.Setup(c => c.GetAsync<GetCategoryByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetCategoryByIdResponse)null!);

            var repo = new Mock<IGenericRepository<Category>>();
            repo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(repo.Object);

            _mockMapper.Setup(m => m.Map<GetCategoryByIdResponse>(category)).Returns(mappedResponse);
            _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<GetCategoryByIdResponse>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(mappedResponse);
            _mockUow.Verify(u => u.Repository<Category>(), Times.AtLeastOnce);
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<GetCategoryByIdResponse>(), CacheTTL.Categories, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_CategoryNotExists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var query = new GetCategoryByIdQuery(categoryId);

            _mockCache.Setup(c => c.GetAsync<GetCategoryByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetCategoryByIdResponse)null!);

            var repo = new Mock<IGenericRepository<Category>>();
            repo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((Category)null!);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(repo.Object);

            _mockMessage.Setup(m => m.GetMessage(MessageKeys.Category.NotFound)).Returns("Category not found");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}

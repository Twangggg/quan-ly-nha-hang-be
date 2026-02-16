using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Categories.Commands.UpdateCategory;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Categories
{
    public class UpdateCategoryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly UpdateCategoryHandler _handler;

        public UpdateCategoryHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();

            _handler = new UpdateCategoryHandler(_mockUow.Object, _mockCache.Object, _mockMessage.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_CategoryUpdated()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var category = new Category { CategoryId = categoryId, Name = "Old Name", CategoryType = CategoryType.Normal };
            var command = new UpdateCategoryCommand(categoryId, "New Name", CategoryType.SpecialGroup);

            var categories = new List<Category> { category }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Category>>();
            repo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(repo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("New Name");
            result.Data.Type.Should().Be(CategoryType.SpecialGroup);
            category.Name.Should().Be("New Name");
            category.CategoryType.Should().Be(CategoryType.SpecialGroup);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(CacheKey.CategoryList, It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CategoryById, categoryId), It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveByPatternAsync("category:list:type:", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_CategoryNotExists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var command = new UpdateCategoryCommand(categoryId, "New Name", CategoryType.Normal);

            var categories = new List<Category>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Category>>();
            repo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(repo.Object);

            _mockMessage.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("Category not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}

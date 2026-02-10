using Moq;
using FluentAssertions;
using FoodHub.Application.Features.Categories.Commands.DeleteCategory;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using FoodHub.Application.Common.Models;

namespace FoodHub.Tests.Features.Categories
{
    public class DeleteCategoryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockUser;
        private readonly DeleteCategoryHandler _handler;

        public DeleteCategoryHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockUser = new Mock<ICurrentUserService>();

            // Mocking IGenericRepository
            var mockRepo = new Mock<IGenericRepository<Category>>();
            _mockUow.Setup(u => u.Repository<Category>()).Returns(mockRepo.Object);

            _handler = new DeleteCategoryHandler(
                _mockUow.Object,
                null!, // IMapper not used in this logic
                _mockUser.Object,
                _mockMessage.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_CategoryNotExists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var command = new DeleteCategoryCommand(categoryId);
            
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

        [Fact]
        public async Task Handle_Should_SoftDeleteCategory_When_Exists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var category = new Category { CategoryId = categoryId, Name = "Đồ uống" };
            var command = new DeleteCategoryCommand(categoryId);

            var categories = new List<Category> { category }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Category>>();
            repo.Setup(r => r.Query()).Returns(categories);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(repo.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            category.DeletedAt.Should().NotBeNull();
            _mockUow.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}

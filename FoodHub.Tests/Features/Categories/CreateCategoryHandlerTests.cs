using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Categories.Commands.CreateCategory;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Moq;

namespace FoodHub.Tests.Features.Categories
{
    public class CreateCategoryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly CreateCategoryHandler _handler;

        public CreateCategoryHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();

            _handler = new CreateCategoryHandler(_mockUow.Object, _mockCache.Object, _mockMessage.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_CategoryCreated()
        {
            // Arrange
            var command = new CreateCategoryCommand("Đồ uống", "DRK", CategoryType.Normal);

            var mockRepo = new Mock<IGenericRepository<Category>>();
            mockRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
                .ReturnsAsync(false);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("Đồ uống");
            result.Data.CodePrefix.Should().Be("DRK");
            result.Data.Type.Should().Be((int)CategoryType.Normal);
            _mockUow.Verify(u => u.Repository<Category>().AddAsync(It.IsAny<Category>()), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(CacheKey.CategoryList, It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveByPatternAsync("category:list:type:", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_CreateCategory_WithComboType()
        {
            // Arrange
            var command = new CreateCategoryCommand("Combo đặc biệt", "CB", CategoryType.Combo);

            var mockRepo = new Mock<IGenericRepository<Category>>();
            mockRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
                .ReturnsAsync(false);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("Combo đặc biệt");
            result.Data.CodePrefix.Should().Be("CB");
            result.Data.Type.Should().Be((int)CategoryType.Combo);
        }


    }
}

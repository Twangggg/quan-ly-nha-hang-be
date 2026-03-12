using System.Linq.Expressions;
using FluentAssertions;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class CreateIngredientHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly CreateIngredientHandler _handler;

        public CreateIngredientHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCurrentUser.SetupGet(x => x.UserId).Returns((string?)null);

            _handler = new CreateIngredientHandler(
                _mockUow.Object,
                _mockMessage.Object,
                _mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CreateIngredientHandler>>(),
                _mockCurrentUser.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_IngredientCreated()
        {
            // Arrange
            var command = new CreateIngredientCommand("ING001", "Hành tây", "Kg", 5, 0, 0, "Hành tây Đà Lạt");

            var mockRepo = new Mock<IGenericRepository<Ingredient>>();
            mockRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(false);

            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Hành tây");
            result.Data.Code.Should().Be("ING001");

            _mockUow.Verify(u => u.Repository<Ingredient>().AddAsync(It.IsAny<Ingredient>()), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CodeExists()
        {
            // Arrange
            var command = new CreateIngredientCommand("ING001", "Hành tây", "Kg", 5, 0, 0);

            var mockRepo = new Mock<IGenericRepository<Ingredient>>();

            // First call for Code check returns true
            mockRepo.Setup(r => r.AnyAsync(It.Is<Expression<Func<Ingredient, bool>>>(e => ExpressionTargetsCode(e))))
                .ReturnsAsync(true);

            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(mockRepo.Object);
            _mockMessage.Setup(m => m.GetMessage("Ingredient.CodeExists")).Returns("Mã nguyên liệu đã tồn tại");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Mã nguyên liệu đã tồn tại");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NameExists()
        {
            // Arrange
            var command = new CreateIngredientCommand("ING002", "Hành tây", "Kg", 5, 0, 0);

            var mockRepo = new Mock<IGenericRepository<Ingredient>>();

            // First call for Code check returns false
            mockRepo.SetupSequence(r => r.AnyAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(false) // Code check
                .ReturnsAsync(true); // Name check

            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(mockRepo.Object);
            _mockMessage.Setup(m => m.GetMessage("Ingredient.NameExists")).Returns("Tên nguyên liệu đã tồn tại");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Tên nguyên liệu đã tồn tại");
        }

        // Helper matcher to avoid brittle ToString checks
        private static bool ExpressionTargetsCode(Expression<Func<Ingredient, bool>> expr)
        {
            // Look for property access of Ingredient.Code in the expression tree
            return expr.Body.ToString().Contains("Code");
        }
    }
}

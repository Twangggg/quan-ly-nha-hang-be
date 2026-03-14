using System.Linq.Expressions;
using FluentAssertions;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GenerateIngredientCode;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class GenerateIngredientCodeHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IGenericRepository<Ingredient>> _mockRepo;
        private readonly GenerateIngredientCodeHandler _handler;

        public GenerateIngredientCodeHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockRepo = new Mock<IGenericRepository<Ingredient>>();

            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(_mockRepo.Object);

            _handler = new GenerateIngredientCodeHandler(
                _mockUow.Object,
                _mockMessageService.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GenerateIngredientCodeHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnGeneratedCode_When_NameIsValid()
        {
            _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(0);

            var result = await _handler.Handle(
                new GenerateIngredientCodeQuery("Hanh tay"),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Code.Should().Be("HANHTAY-1");
        }

        [Fact]
        public async Task Handle_Should_ReturnGeneratedCodeWithNextGlobalSequence_When_PreviousIngredientsExist()
        {
            _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(
                new GenerateIngredientCodeQuery("Hanh tay"),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Code.Should().Be("HANHTAY-2");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NameIsEmpty()
        {
            _mockMessageService
                .Setup(m => m.GetMessage("Ingredient.NameRequired"))
                .Returns("Ten nguyen lieu la bat buoc");

            var result = await _handler.Handle(
                new GenerateIngredientCodeQuery(" "),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Ten nguyen lieu la bat buoc");
            _mockRepo.Verify(
                r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>()),
                Times.Never
            );
        }
    }
}

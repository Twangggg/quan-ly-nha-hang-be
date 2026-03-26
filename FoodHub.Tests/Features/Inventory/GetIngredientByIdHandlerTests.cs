using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredientById;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetIngredientByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly IMapper _mapper;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly GetIngredientByIdHandler _handler;

        public GetIngredientByIdHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Ingredient, GetIngredientByIdResponse>()
                    .ForMember(d => d.StockStatus, opt => opt.Ignore())
                    .ForMember(d => d.InventoryGroupName, opt => opt.MapFrom(s => s.InventoryGroup != null ? s.InventoryGroup.Name : null));
            }, mockLoggerFactory.Object);
            _mapper = config.CreateMapper();

            _handler = new GetIngredientByIdHandler(
                _mockUow.Object,
                _mapper,
                _mockCache.Object,
                _mockMessage.Object,
                Mock.Of<ILogger<GetIngredientByIdHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_Missing()
        {
            // Arrange
            var query = new GetIngredientByIdQuery(Guid.NewGuid());

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);
            _mockMessage.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("Ingredient not found");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnIngredient_When_Found()
        {
            // Arrange
            var ingredientId = Guid.NewGuid();
            var ingredient = Ingredient.Create("ING005", "Tiêu", "Gram", 50, 0, 0, "Tiêu sọ");
            typeof(Ingredient).GetProperty("IngredientId")!.SetValue(ingredient, ingredientId);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);

            var query = new GetIngredientByIdQuery(ingredientId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IngredientId.Should().Be(ingredientId);
            result.Data.Name.Should().Be("Tiêu");
        }
    }
}

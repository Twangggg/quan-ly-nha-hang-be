using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetIngredientsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly GetIngredientsHandler _handler;

        public GetIngredientsHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            _handler = new GetIngredientsHandler(
                _mockUow.Object,
                _mockMapper.Object,
                Mock.Of<ILogger<GetIngredientsHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedResult()
        {
            // Arrange
            var ingredients = new List<Ingredient>
            {
                Ingredient.Create("ING001", "Muối", "Kg", 1, 0, 0),
                Ingredient.Create("ING002", "Đường", "Kg", 2, 0, 0)
            };

            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var query = new GetIngredientsQuery(pagination);

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(r => r.Query()).Returns(ingredients.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Ingredient>()).Returns(repo.Object);

            var mapperConfig = new MapperConfiguration(cfg => cfg.CreateMap<Ingredient, GetIngredientsResponse>());
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mapperConfig);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }
    }
}

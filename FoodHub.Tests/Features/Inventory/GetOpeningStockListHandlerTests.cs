using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.OpeningStock.Queries.GetOpeningStockList;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetOpeningStockListHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnPagedActiveIngredients()
        {
            var active = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 2, null);
            var activeSecond = Ingredient.Create("ING003", "Sugar", "Kg", 0, 8, 3, null);
            var inactive = Ingredient.Create("ING002", "Pepper", "Kg", 0, 5, 1, null);

            inactive.Update(
                inactive.Name,
                inactive.BaseUnit,
                inactive.LowStockThreshold,
                inactive.Description,
                false,
                inactive.Code,
                inactive.CurrentStock,
                inactive.CostPrice
            );

            var repo = new Mock<IGenericRepository<Ingredient>>();
            repo.Setup(x => x.Query())
                .Returns(
                    new List<Ingredient> { active, activeSecond, inactive }
                        .AsQueryable()
                        .BuildMock()
                );

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(x => x.Repository<Ingredient>()).Returns(repo.Object);

            var handler = new GetOpeningStockListHandler(
                mockUow.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetOpeningStockListHandler>>()
            );

            var result = await handler.Handle(
                new GetOpeningStockListQuery(new PaginationParams { PageNumber = 1, PageSize = 1 }),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.TotalCount.Should().Be(2);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(1);
            result.Data.Items[0].Code.Should().Be("ING001");
        }
    }
}

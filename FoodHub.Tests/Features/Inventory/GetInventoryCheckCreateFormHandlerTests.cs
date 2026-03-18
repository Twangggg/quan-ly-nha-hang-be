using FluentAssertions;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckCreateForm;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetInventoryCheckCreateFormHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnActiveIngredients_SortedByName()
        {
            var bIngredient = Ingredient.Create("ING002", "Salt", "Kg", 0, 10, 3, null);
            var aIngredient = Ingredient.Create("ING001", "Chili", "Kg", 0, 5, 2, null);
            var inactiveIngredient = Ingredient.Create("ING003", "Pepper", "Kg", 0, 8, 4, null);
            inactiveIngredient.Deactivate(false);

            var mockRepo = new Mock<IGenericRepository<Ingredient>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(mockRepo.Object);
            mockRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<Ingredient> { bIngredient, aIngredient, inactiveIngredient }
                        .AsQueryable()
                        .BuildMock()
                );

            var handler = new GetInventoryCheckCreateFormHandler(
                mockUnitOfWork.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryCheckCreateFormHandler>>()
            );

            var result = await handler.Handle(new GetInventoryCheckCreateFormQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data!.Select(x => x.IngredientName).Should().ContainInOrder("Chili", "Salt");
        }
    }
}

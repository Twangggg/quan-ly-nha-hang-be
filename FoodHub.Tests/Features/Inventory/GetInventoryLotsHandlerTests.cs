using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Lots.Queries.GetInventoryLots;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using System.Reflection;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetInventoryLotsHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnPagedLotsOrderedByExpiry()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCache = new Mock<ICacheService>();
            var mockLotRepo = new Mock<IGenericRepository<InventoryLot>>();
            var mockSettingsRepo = new Mock<IGenericRepository<InventorySettings>>();

            var ingredientA = Ingredient.Create("ING001", "Beef", "Kg", 3, 12, 10, null);
            var ingredientB = Ingredient.Create("ING002", "Onion", "Kg", 2, 6, 5, null);

            var nearExpiryLot = InventoryLot.Create(
                ingredientA.IngredientId,
                Guid.NewGuid(),
                "LOT-001",
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(2),
                10,
                4
            ).Value!;
            var activeLot = InventoryLot.Create(
                ingredientB.IngredientId,
                Guid.NewGuid(),
                "LOT-002",
                DateTime.UtcNow.AddDays(-3),
                DateTime.UtcNow.AddDays(20),
                5,
                3
            ).Value!;

            SetIngredient(nearExpiryLot, ingredientA);
            SetIngredient(activeLot, ingredientB);

            mockUnitOfWork.Setup(x => x.Repository<InventoryLot>()).Returns(mockLotRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventorySettings>())
                .Returns(mockSettingsRepo.Object);

            mockLotRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryLot> { activeLot, nearExpiryLot }.AsQueryable().BuildMock());
            mockSettingsRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<InventorySettings> { InventorySettings.CreateDefault() }
                        .AsQueryable()
                        .BuildMock()
                );

            var handler = new GetInventoryLotsHandler(
                mockUnitOfWork.Object,
                mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryLotsHandler>>()
            );

            var result = await handler.Handle(
                new GetInventoryLotsQuery(new PaginationParams { PageNumber = 1, PageSize = 10 }),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items[0].LotCode.Should().Be("LOT-001");
            result.Data.Items[0].Status.Should().Be(InventoryLotStatus.NearExpiry);
            result.Data.Items[1].LotCode.Should().Be("LOT-002");
            result.Data.Items[1].Status.Should().Be(InventoryLotStatus.Active);
        }

        [Fact]
        public async Task Handle_Should_FilterLotsBySearchTerm()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCache = new Mock<ICacheService>();
            var mockLotRepo = new Mock<IGenericRepository<InventoryLot>>();
            var mockSettingsRepo = new Mock<IGenericRepository<InventorySettings>>();

            var ingredientA = Ingredient.Create("ING001", "Beef", "Kg", 3, 12, 10, null);
            var ingredientB = Ingredient.Create("ING002", "Onion", "Kg", 2, 6, 5, null);

            var beefLot = InventoryLot.Create(
                ingredientA.IngredientId,
                Guid.NewGuid(),
                "BEEF-LOT",
                DateTime.UtcNow.AddDays(-2),
                DateTime.UtcNow.AddDays(10),
                10,
                4
            ).Value!;
            var onionLot = InventoryLot.Create(
                ingredientB.IngredientId,
                Guid.NewGuid(),
                "ONION-LOT",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(12),
                5,
                3
            ).Value!;

            SetIngredient(beefLot, ingredientA);
            SetIngredient(onionLot, ingredientB);

            mockUnitOfWork.Setup(x => x.Repository<InventoryLot>()).Returns(mockLotRepo.Object);
            mockUnitOfWork
                .Setup(x => x.Repository<InventorySettings>())
                .Returns(mockSettingsRepo.Object);

            mockLotRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryLot> { beefLot, onionLot }.AsQueryable().BuildMock());
            mockSettingsRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<InventorySettings> { InventorySettings.CreateDefault() }
                        .AsQueryable()
                        .BuildMock()
                );

            var handler = new GetInventoryLotsHandler(
                mockUnitOfWork.Object,
                mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryLotsHandler>>()
            );

            var result = await handler.Handle(
                new GetInventoryLotsQuery(
                    new PaginationParams { PageNumber = 1, PageSize = 10, Search = "beef" }
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().ContainSingle();
            result.Data.Items[0].LotCode.Should().Be("BEEF-LOT");
            result.Data.Items[0].IngredientName.Should().Be("Beef");
        }

        [Fact]
        public async Task Handle_Should_ReturnCachedLots_WhenCacheExists()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCache = new Mock<ICacheService>();

            var cachedResult = new PagedResult<GetInventoryLotsResponse>(
                new List<GetInventoryLotsResponse>
                {
                    new()
                    {
                        InventoryLotId = Guid.NewGuid(),
                        IngredientId = Guid.NewGuid(),
                        IngredientCode = "ING001",
                        IngredientName = "Beef",
                        LotCode = "LOT-CACHED",
                        Unit = "Kg",
                        Status = InventoryLotStatus.Active,
                    },
                },
                new PaginationParams { PageNumber = 1, PageSize = 10 },
                1
            );

            mockCache
                .Setup(
                    x =>
                        x.GetAsync<PagedResult<GetInventoryLotsResponse>>(
                            It.IsAny<string>(),
                            It.IsAny<CancellationToken>()
                        )
                )
                .ReturnsAsync(cachedResult);

            var handler = new GetInventoryLotsHandler(
                mockUnitOfWork.Object,
                mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryLotsHandler>>()
            );

            var result = await handler.Handle(
                new GetInventoryLotsQuery(new PaginationParams { PageNumber = 1, PageSize = 10 }),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedResult);
            mockUnitOfWork.Verify(x => x.Repository<InventoryLot>(), Times.Never);
        }

        private static void SetIngredient(InventoryLot lot, Ingredient ingredient)
        {
            typeof(InventoryLot)
                .GetProperty(
                    nameof(InventoryLot.Ingredient),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )!
                .SetValue(lot, ingredient);
        }
    }
}

using FluentAssertions;
using FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetInventoryLedgerHandlerTests
    {
        private readonly GetInventoryLedgerHandler _handler;
        private readonly Mock<IGenericRepository<InventoryTransaction>> _mockTransactionRepo;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public GetInventoryLedgerHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();

            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(_mockTransactionRepo.Object);

            _handler = new GetInventoryLedgerHandler(
                _mockUnitOfWork.Object,
                _mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryLedgerHandler>>()
            );
        }

        private static void SetProperty<T>(T target, string propertyName, object? value)
        {
            target
                ?.GetType()
                .GetProperty(propertyName)!
                .SetValue(target, value);
        }

        [Fact]
        public async Task Handle_Should_FilterByTransactionType_AndReturnLedgerProjection()
        {
            var ingredient = Ingredient.Create("ING-01", "Salt", "kg", 0, 10, 1, null);
            var ingredientId = ingredient.IngredientId;
            var inventoryCheckTransaction = InventoryTransaction.CreateInventoryCheck(
                ingredientId,
                -2,
                4,
                8,
                "CHECK-01"
            );
            SetDate(
                inventoryCheckTransaction,
                "OccurredAt",
                new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc)
            );

            var stockInTransaction = InventoryTransaction.CreateStockIn(
                ingredientId,
                5,
                4,
                10,
                "NK-01"
            );

            SetProperty(inventoryCheckTransaction, "Ingredient", ingredient);
            SetProperty(stockInTransaction, "Ingredient", ingredient);
            SetDate(stockInTransaction, "OccurredAt", new DateTime(2026, 3, 10, 7, 0, 0, DateTimeKind.Utc));

            _mockTransactionRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<InventoryTransaction>
                    {
                        inventoryCheckTransaction,
                        stockInTransaction,
                    }
                    .AsQueryable()
                    .BuildMock()
                );

            var result = await _handler.Handle(
                new GetInventoryLedgerQuery(
                    ingredientId,
                    new DateOnly(2026, 3, 10),
                    new DateOnly(2026, 3, 10),
                    InventoryTransactionType.InventoryCheck
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().ContainSingle();
            var item = result.Data.Items.Single();
            item.TransactionType.Should().Be(InventoryTransactionType.InventoryCheck);
            item.ReferenceNo.Should().Be("CHECK-01");
            item.QuantityDelta.Should().Be(-2);
            item.BalanceAfter.Should().Be(8);
        }

        private static void SetDate(object target, string propertyName, DateTime value)
        {
            target
                .GetType()
                .GetProperty(propertyName)!
                .SetValue(target, value);
        }
    }
}

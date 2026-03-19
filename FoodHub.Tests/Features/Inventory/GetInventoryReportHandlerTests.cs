using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetInventoryReportHandlerTests
    {
        private readonly GetInventoryReportHandler _handler;
        private readonly Mock<IGenericRepository<Ingredient>> _mockIngredientRepo;
        private readonly Mock<IGenericRepository<InventoryTransaction>> _mockTransactionRepo;
        private readonly Mock<IGenericRepository<StockInReceiptItem>> _mockStockInReceiptItemRepo;
        private readonly Mock<IGenericRepository<StockOutReceiptItem>> _mockStockOutReceiptItemRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public GetInventoryReportHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _mockTransactionRepo = new Mock<IGenericRepository<InventoryTransaction>>();
            _mockStockInReceiptItemRepo = new Mock<IGenericRepository<StockInReceiptItem>>();
            _mockStockOutReceiptItemRepo = new Mock<IGenericRepository<StockOutReceiptItem>>();

            _mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(_mockIngredientRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryTransaction>())
                .Returns(_mockTransactionRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<StockInReceiptItem>())
                .Returns(_mockStockInReceiptItemRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<StockOutReceiptItem>())
                .Returns(_mockStockOutReceiptItemRepo.Object);

            _handler = new GetInventoryReportHandler(
                _mockUnitOfWork.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryReportHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_CalculateInventoryReport_ByFormula()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 10, 4, null);
            var openingTransaction = InventoryTransaction.CreateOpeningStock(
                ingredient.IngredientId,
                10,
                4,
                10
            );
            SetDate(openingTransaction, "OccurredAt", new DateTime(2026, 3, 9, 10, 0, 0, DateTimeKind.Utc));

            var saleDeductionTransaction = InventoryTransaction.CreateSaleDeduction(
                ingredient.IngredientId,
                2,
                4,
                10,
                "ORDER-01"
            );
            SetDate(saleDeductionTransaction, "OccurredAt", new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc));

            var stockInReceipt = StockInReceipt.CreateInventoryAdjustment(
                "NK-20260310-0001",
                new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
                "Adjustment"
            );
            stockInReceipt.AddItem(ingredient.IngredientId, 5, ingredient.BaseUnit, 4, null, null);
            SetReference(stockInReceipt.Items.Single(), "StockInReceipt", stockInReceipt);

            var stockOutReceipt = StockOutReceipt.CreateInventoryAdjustment(
                "XK-20260310-0001",
                new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                "Adjustment"
            );
            stockOutReceipt.AddItem(ingredient.IngredientId, 3, 4, null);
            SetReference(stockOutReceipt.Items.Single(), "StockOutReceipt", stockOutReceipt);

            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockTransactionRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<InventoryTransaction>
                    {
                        openingTransaction,
                        saleDeductionTransaction,
                    }.AsQueryable().BuildMock()
                );
            _mockStockInReceiptItemRepo
                .Setup(x => x.Query())
                .Returns(stockInReceipt.Items.AsQueryable().BuildMock());
            _mockStockOutReceiptItemRepo
                .Setup(x => x.Query())
                .Returns(stockOutReceipt.Items.AsQueryable().BuildMock());

            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var result = await _handler.Handle(
                new GetInventoryReportQuery(
                    pagination,
                    new DateOnly(2026, 3, 10),
                    new DateOnly(2026, 3, 10),
                    ingredient.IngredientId
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().ContainSingle();
            var report = result.Data!.Items.Single();
            report.OpeningStock.Should().Be(10);
            report.TotalStockIn.Should().Be(5);
            report.TotalStockOut.Should().Be(3);
            report.TotalSaleDeduction.Should().Be(2);
            report.TotalOutbound.Should().Be(3); // StockOut only, SaleDeduction is display only (not double counted)
            report.ClosingStock.Should().Be(12); // 10 + 5 - 3 = 12
            report.AverageUnitCost.Should().Be(4);
            report.ClosingStockValue.Should().Be(48); // 12 * 4 = 48
        }

        private static void SetDate(object target, string propertyName, DateTime value)
        {
            target
                .GetType()
                .GetProperty(propertyName)!
                .SetValue(target, value);
        }

        private static void SetReference(object target, string propertyName, object value)
        {
            target
                .GetType()
                .GetProperty(propertyName)!
                .SetValue(target, value);
        }
    }
}

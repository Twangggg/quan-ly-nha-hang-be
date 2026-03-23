using FluentAssertions;
using FoodHub.Application.Features.Dashboard.Orders.Queries.GetOrderDashboardOverview;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Dashboard
{
    public class GetOrderDashboardOverviewHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnOrderDashboardSummary()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockOrderRepo = new Mock<IGenericRepository<Order>>();
            var mockTableRepo = new Mock<IGenericRepository<Table>>();

            var area = new Area { AreaId = Guid.NewGuid(), Name = "Tang 1", CodePrefix = "T1" };
            var dineInTable = new Table
            {
                TableId = Guid.NewGuid(),
                TableNumber = 1,
                Status = TableStatus.Available,
                Area = area,
            };
            var cleaningTable = new Table
            {
                TableId = Guid.NewGuid(),
                TableNumber = 2,
                Status = TableStatus.Cleaning,
                Area = area,
            };
            var availableTable = new Table
            {
                TableId = Guid.NewGuid(),
                TableNumber = 3,
                Status = TableStatus.Available,
                Area = area,
            };

            var servingOrder = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = "ORD-001",
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TableId = dineInTable.TableId,
                Table = dineInTable,
                IsPriority = true,
                TotalAmount = 100_000,
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                OrderItems =
                [
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        Status = OrderItemStatus.Preparing,
                        Quantity = 2,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-19),
                    },
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        Status = OrderItemStatus.Ready,
                        Quantity = 1,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-18),
                    },
                ],
            };

            var waitingCheckoutOrder = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = "ORD-002",
                Status = OrderStatus.Serving,
                OrderType = OrderType.Takeaway,
                IsPriority = false,
                TotalAmount = 80_000,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                OrderItems =
                [
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        Status = OrderItemStatus.Completed,
                        Quantity = 1,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-9),
                    },
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        Status = OrderItemStatus.Cancelled,
                        Quantity = 1,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-8),
                    },
                ],
            };

            var paidOrderToday = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = "ORD-003",
                Status = OrderStatus.Paid,
                OrderType = OrderType.Delivery,
                TotalAmount = 150_000,
                PaidAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            };

            mockUnitOfWork.Setup(x => x.Repository<Order>()).Returns(mockOrderRepo.Object);
            mockUnitOfWork.Setup(x => x.Repository<Table>()).Returns(mockTableRepo.Object);

            mockOrderRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<Order> { servingOrder, waitingCheckoutOrder, paidOrderToday }
                        .AsQueryable()
                        .BuildMock()
                );
            mockTableRepo
                .Setup(x => x.Query())
                .Returns(
                    new List<Table> { dineInTable, cleaningTable, availableTable }
                        .AsQueryable()
                        .BuildMock()
                );

            var handler = new GetOrderDashboardOverviewHandler(
                mockUnitOfWork.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetOrderDashboardOverviewHandler>>()
            );

            var result = await handler.Handle(
                new GetOrderDashboardOverviewQuery(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ActiveOrders.Should().Be(2);
            result.Data.PriorityOrders.Should().Be(1);
            result.Data.DineInOrders.Should().Be(1);
            result.Data.TakeawayOrders.Should().Be(1);
            result.Data.DeliveryOrders.Should().Be(0);
            result.Data.OccupiedTables.Should().Be(1);
            result.Data.AvailableTables.Should().Be(1);
            result.Data.CleaningTables.Should().Be(1);
            result.Data.PendingKitchenItems.Should().Be(1);
            result.Data.CookingItems.Should().Be(0);
            result.Data.ReadyItems.Should().Be(1);
            result.Data.WaitingCheckoutOrders.Should().Be(1);
            result.Data.TodayPaidOrders.Should().Be(1);
            result.Data.TodayRevenue.Should().Be(150_000);
            result.Data.TopActiveOrders.Should().HaveCount(2);
            result.Data.TopActiveOrders.First().TableLabel.Should().Be("T1_1");
            result.Data.StatusBreakdown.Should().Contain(x => x.Status == nameof(OrderStatus.Serving) && x.Count == 2);
            result.Data.StatusBreakdown.Should().Contain(x => x.Status == nameof(OrderStatus.Paid) && x.Count == 1);
        }
    }
}

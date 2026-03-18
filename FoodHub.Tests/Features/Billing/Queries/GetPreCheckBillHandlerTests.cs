using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Billing.Queries
{
    public class GetPreCheckBillHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<IMessageService> _mockMessageService = new();
        private readonly Mock<ILogger<GetPreCheckBillHandler>> _mockLogger = new();

        private GetPreCheckBillHandler CreateHandler() =>
            new(_mockUow.Object, _mockMessageService.Object, _mockLogger.Object);

        private void SetupOrderRepo(List<FoodHub.Domain.Entities.Order> orders)
        {
            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<FoodHub.Domain.Entities.Order>()).Returns(mockRepo.Object);
        }

        private static FoodHub.Domain.Entities.Order CreateServingOrder(
            Guid? orderId = null,
            int tableNumber = 5,
            string employeeName = "Nguyễn Văn A"
        )
        {
            var id = orderId ?? Guid.NewGuid();
            return new FoodHub.Domain.Entities.Order
            {
                OrderId = id,
                OrderCode = "ORD-20260314-0001",
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TableId = Guid.NewGuid(),
                Table = new Table { TableNumber = tableNumber },
                CreatedByEmployee = new Employee { FullName = employeeName },
                TotalAmount = 100000,
                OrderItems = new List<OrderItem>(),
            };
        }

        private static OrderItem CreateOrderItem(
            OrderItemStatus status = OrderItemStatus.Preparing,
            string name = "Phở Bò",
            int quantity = 1,
            decimal unitPrice = 50000
        )
        {
            return new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                ItemNameSnapshot = name,
                ItemCodeSnapshot = "PHO01",
                StationSnapshot = "Kitchen",
                Status = status,
                Quantity = quantity,
                UnitPriceSnapshot = unitPrice,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderHasValidItems()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = CreateServingOrder(orderId);
            order.OrderItems.Add(CreateOrderItem(name: "Phở Bò", quantity: 2, unitPrice: 50000));
            order.OrderItems.Add(CreateOrderItem(name: "Cơm Gà", quantity: 1, unitPrice: 45000));

            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order> { order });

            var handler = CreateHandler();
            var query = new GetPreCheckBillQuery { OrderId = orderId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.OrderId.Should().Be(orderId);
            result.Data.OrderCode.Should().Be("ORD-20260314-0001");
            result.Data.TableNumber.Should().Be(5);
            result.Data.EmployeeName.Should().Be("Nguyễn Văn A");
            result.Data.Items.Should().HaveCount(2);
            result.Data.SubTotal.Should().Be(145000);
            result.Data.TotalAmount.Should().Be(145000);
            result.Data.Discount.Should().Be(0);
            result.Data.Vat.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_OrderDoesNotExist()
        {
            // Arrange
            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order>());
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.NotFound))
                .Returns("Order not found");

            var handler = CreateHandler();
            var query = new GetPreCheckBillQuery { OrderId = Guid.NewGuid() };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Theory]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Completed)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task Handle_Should_ReturnFailure_When_OrderNotServing(OrderStatus status)
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = CreateServingOrder(orderId);
            order.Status = status;
            order.OrderItems.Add(CreateOrderItem());

            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order> { order });
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.InvalidActionWithStatus, It.IsAny<object>()))
                .Returns("Invalid action");

            var handler = CreateHandler();
            var query = new GetPreCheckBillQuery { OrderId = orderId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AllItemsCancelled()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = CreateServingOrder(orderId);
            order.OrderItems.Add(CreateOrderItem(status: OrderItemStatus.Cancelled));
            order.OrderItems.Add(CreateOrderItem(status: OrderItemStatus.Rejected));

            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order> { order });
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.NoValidItems))
                .Returns("No valid items");

            var handler = CreateHandler();
            var query = new GetPreCheckBillQuery { OrderId = orderId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ExcludeCancelledItems_FromTotal()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = CreateServingOrder(orderId);
            order.OrderItems.Add(CreateOrderItem(name: "Phở Bò", quantity: 1, unitPrice: 50000));
            order.OrderItems.Add(
                CreateOrderItem(
                    name: "Bánh Mì",
                    quantity: 2,
                    unitPrice: 25000,
                    status: OrderItemStatus.Cancelled
                )
            );
            order.OrderItems.Add(
                CreateOrderItem(
                    name: "Cơm Sườn",
                    quantity: 1,
                    unitPrice: 60000,
                    status: OrderItemStatus.Rejected
                )
            );

            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order> { order });

            var handler = CreateHandler();
            var query = new GetPreCheckBillQuery { OrderId = orderId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items[0].ItemName.Should().Be("Phở Bò");
            result.Data.SubTotal.Should().Be(50000);
            result.Data.TotalAmount.Should().Be(50000);
        }
    }
}

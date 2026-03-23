using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder;
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
using EntityOrder = FoodHub.Domain.Entities.Order;

namespace FoodHub.Tests.Features.MergeSplitOrder.Commands
{
    public class SplitOrderHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
        private readonly Mock<IMessageService> _mockMessageService = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ILogger<SplitOrderHandler>> _mockLogger = new();

        [Fact]
        public async Task Handle_Should_Create_New_Order_On_Destination_Table_And_Close_Source_When_All_Items_Are_Moved()
        {
            var userId = Guid.NewGuid();
            var sourceOrderId = Guid.NewGuid();
            var sourceTableId = Guid.NewGuid();
            var destinationTableId = Guid.NewGuid();

            var sourceOrder = new EntityOrder
            {
                OrderId = sourceOrderId,
                OrderCode = "ORD-SRC",
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = sourceTableId,
                OrderItems = new List<OrderItem>(),
            };

            var sourceItem = CreateOrderItem(sourceOrderId, Guid.NewGuid(), 2, 12m, OrderItemStatus.Cooking);
            sourceOrder.OrderItems.Add(sourceItem);
            sourceOrder.RecalculateTotalAmount();

            var sourceTable = new Table
            {
                TableId = sourceTableId,
                TableNumber = 1,
                Status = TableStatus.Occupied,
                Orders = new List<EntityOrder> { sourceOrder },
            };

            var destinationTable = new Table
            {
                TableId = destinationTableId,
                TableNumber = 2,
                Status = TableStatus.Available,
                Orders = new List<EntityOrder>(),
            };

            var orders = new List<EntityOrder> { sourceOrder };
            var tables = new List<Table> { sourceTable, destinationTable };

            var orderRepo = new Mock<IGenericRepository<EntityOrder>>();
            orderRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            orderRepo
                .Setup(r => r.AddAsync(It.IsAny<EntityOrder>()))
                .Callback<EntityOrder>(order => orders.Add(order))
                .Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.Repository<EntityOrder>()).Returns(orderRepo.Object);

            var orderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(orderItemRepo.Object);

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(r => r.Query()).Returns(tables.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);

            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockMapper
                .Setup(m => m.Map<List<SplitOrderItemDto>>(It.IsAny<List<OrderItem>>()))
                .Returns((List<OrderItem> items) =>
                    items
                        .Select(item => new SplitOrderItemDto
                        {
                            OrderItemId = item.OrderItemId,
                            Quantity = item.Quantity,
                        })
                        .ToList()
                );

            var handler = new SplitOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new SplitOrderCommand(
                    SourceOrderId: sourceOrderId,
                    DestinationOrderId: null,
                    DestinationTableId: destinationTableId,
                    DestinationReservationId: null,
                    ItemsToSplit:
                    new List<SplitOrderItemCommand>
                    {
                        new(sourceItem.OrderItemId, 2),
                    }
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CreatedNewOrder.Should().BeTrue();
            result.Data.SourceOrderItems.Should().BeEmpty();
            result.Data.DestinationOrderItems.Should().ContainSingle();
            sourceOrder.Status.Should().Be(OrderStatus.Closed);
            sourceOrder.TotalAmount.Should().Be(0);
            destinationTable.Status.Should().Be(TableStatus.Occupied);
            sourceTable.Status.Should().Be(TableStatus.Available);
            result.Data.DestinationOrderTotalAmount.Should().Be(26.40m); // 24 * 1.1 = 26.40 -> rounded to 26
            result.Data.DestinationOrderItems.Single().Quantity.Should().Be(2);
            auditRepo.Verify(r => r.AddAsync(It.IsAny<OrderAuditLog>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Item_Is_Completed()
        {
            var userId = Guid.NewGuid();
            var sourceOrderId = Guid.NewGuid();
            var destinationTableId = Guid.NewGuid();

            var sourceOrder = new EntityOrder
            {
                OrderId = sourceOrderId,
                OrderCode = "ORD-SRC",
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = Guid.NewGuid(),
                OrderItems = new List<OrderItem>(),
            };

            var sourceItem = CreateOrderItem(
                sourceOrderId,
                Guid.NewGuid(),
                1,
                8m,
                OrderItemStatus.Completed
            );
            sourceOrder.OrderItems.Add(sourceItem);

            var orderRepo = new Mock<IGenericRepository<EntityOrder>>();
            orderRepo
                .Setup(r => r.Query())
                .Returns(new List<EntityOrder> { sourceOrder }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<EntityOrder>()).Returns(orderRepo.Object);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.InvalidActionWithStatus))
                .Returns("Invalid order status");

            var handler = new SplitOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new SplitOrderCommand(
                    SourceOrderId: sourceOrderId,
                    DestinationOrderId: null,
                    DestinationTableId: destinationTableId,
                    DestinationReservationId: null,
                    ItemsToSplit:
                    new List<SplitOrderItemCommand>
                    {
                        new(sourceItem.OrderItemId, 1),
                    }
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Destination_Order_Is_The_Same_As_Source()
        {
            var userId = Guid.NewGuid();
            var sourceOrderId = Guid.NewGuid();

            var sourceOrder = new EntityOrder
            {
                OrderId = sourceOrderId,
                OrderCode = "ORD-SRC",
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = Guid.NewGuid(),
                OrderItems = new List<OrderItem>(),
            };

            sourceOrder.OrderItems.Add(
                CreateOrderItem(sourceOrderId, Guid.NewGuid(), 1, 8m, OrderItemStatus.Cooking)
            );

            var orderRepo = new Mock<IGenericRepository<EntityOrder>>();
            orderRepo
                .Setup(r => r.Query())
                .Returns(new List<EntityOrder> { sourceOrder }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<EntityOrder>()).Returns(orderRepo.Object);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.InvalidAction))
                .Returns("Invalid action");

            var handler = new SplitOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new SplitOrderCommand(
                    SourceOrderId: sourceOrderId,
                    DestinationOrderId: sourceOrderId,
                    DestinationTableId: null,
                    DestinationReservationId: null,
                    ItemsToSplit: new List<SplitOrderItemCommand>
                    {
                        new(sourceOrder.OrderItems.Single().OrderItemId, 1),
                    }
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        private static OrderItem CreateOrderItem(
            Guid orderId,
            Guid menuItemId,
            int quantity,
            decimal price,
            OrderItemStatus status
        ) =>
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPriceSnapshot = price,
                Status = status,
                ItemCodeSnapshot = "ITEM",
                ItemNameSnapshot = "Item",
                StationSnapshot = "Kitchen",
                OptionGroups = new List<OrderItemOptionGroup>(),
            };
    }
}

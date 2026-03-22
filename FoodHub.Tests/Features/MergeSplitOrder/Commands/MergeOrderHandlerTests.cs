using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder;
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

namespace FoodHub.Tests.Features.MergeSplitOrder.Commands
{
    public class MergeOrderHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
        private readonly Mock<IMessageService> _mockMessageService = new();
        private readonly Mock<ICacheService> _mockCacheService = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ILogger<MergeOrderHandler>> _mockLogger = new();

        [Fact]
        public async Task Handle_Should_Merge_Orders_And_Mark_Secondary_Order_AsMerged()
        {
            var userId = Guid.NewGuid();
            var firstOrderId = Guid.NewGuid();
            var secondOrderId = Guid.NewGuid();
            var firstTableId = Guid.NewGuid();
            var secondTableId = Guid.NewGuid();

            var firstOrder = CreateServingOrder(firstOrderId, "ORD-1", firstTableId);
            var secondOrder = CreateServingOrder(secondOrderId, "ORD-2", secondTableId);

            firstOrder.OrderItems.Add(CreateOrderItem(firstOrderId, Guid.NewGuid(), 1, 10m, "Pho"));
            secondOrder.OrderItems.Add(
                CreateOrderItem(secondOrderId, firstOrder.OrderItems.First().MenuItemId, 2, 10m, "Pho")
            );
            firstOrder.RecalculateTotalAmount();
            secondOrder.RecalculateTotalAmount();

            var secondTable = new Table
            {
                TableId = secondTableId,
                TableNumber = 2,
                Status = TableStatus.Occupied,
                Orders = new List<Domain.Entities.Order> { secondOrder },
            };

            var tables = new List<Table>
            {
                new()
                {
                    TableId = firstTableId,
                    TableNumber = 1,
                    Status = TableStatus.Occupied,
                    Orders = new List<Domain.Entities.Order> { firstOrder },
                },
                secondTable,
            };

            var orders = new List<Domain.Entities.Order> { firstOrder, secondOrder };
            var orderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            orderRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Order>()).Returns(orderRepo.Object);

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
                .Setup(m => m.Map<List<OrderItemDto>>(It.IsAny<List<OrderItem>>()))
                .Returns((List<OrderItem> items) =>
                    items
                        .Select(item => new OrderItemDto
                        {
                            OrderItemId = item.OrderItemId,
                            MenuItemId = item.MenuItemId,
                            ItemNameSnapshot = item.ItemNameSnapshot,
                            Quantity = item.Quantity,
                            PriceSnapshot = item.UnitPriceSnapshot,
                            ItemNote = item.ItemNote ?? string.Empty,
                            TotalPrice = item.GetTotalPrice(),
                        })
                        .ToList()
                );

            var handler = new MergeOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new MergeOrderCommand(firstOrderId, secondOrderId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            firstOrder.TotalAmount.Should().Be(33m); // 30 * 1.1 = 33
            firstOrder.OrderItems.Should().ContainSingle();
            firstOrder.OrderItems.First().Quantity.Should().Be(3);
            secondOrder.Status.Should().Be(OrderStatus.Merged);
            secondTable.Status.Should().Be(TableStatus.Available);
            orderItemRepo.Verify(r => r.Delete(It.IsAny<OrderItem>()), Times.Once);
            auditRepo.Verify(r => r.AddAsync(It.IsAny<OrderAuditLog>()), Times.Once);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync("table:list*", It.IsAny<CancellationToken>()),
                Times.Once
            );
            _mockCacheService.Verify(
                c => c.RemoveByPatternAsync("table:area:*", It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Orders_Are_The_Same()
        {
            var userId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.InvalidAction))
                .Returns("Invalid action");

            var handler = new MergeOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var orderId = Guid.NewGuid();
            var result = await handler.Handle(
                new MergeOrderCommand(orderId, orderId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Secondary_Order_Does_Not_Exist()
        {
            var userId = Guid.NewGuid();
            var firstOrderId = Guid.NewGuid();
            var secondOrderId = Guid.NewGuid();

            var firstOrder = CreateServingOrder(firstOrderId, "ORD-1", Guid.NewGuid());
            var orders = new List<Domain.Entities.Order> { firstOrder };

            var orderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            orderRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Order>()).Returns(orderRepo.Object);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.NotFound, secondOrderId))
                .Returns("Order not found");

            var handler = new MergeOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new MergeOrderCommand(firstOrderId, secondOrderId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Merge_Completed_Items_Without_Blocking()
        {
            var userId = Guid.NewGuid();
            var firstOrderId = Guid.NewGuid();
            var secondOrderId = Guid.NewGuid();

            var firstOrder = CreateServingOrder(firstOrderId, "ORD-1", Guid.NewGuid());
            var secondOrder = CreateServingOrder(secondOrderId, "ORD-2", Guid.NewGuid());

            firstOrder.OrderItems.Add(CreateOrderItem(firstOrderId, Guid.NewGuid(), 1, 10m, "Pho"));
            secondOrder.OrderItems.Add(
                CreateOrderItem(secondOrderId, Guid.NewGuid(), 1, 12m, "Bun", OrderItemStatus.Completed)
            );

            firstOrder.RecalculateTotalAmount();
            secondOrder.RecalculateTotalAmount();

            var orders = new List<Domain.Entities.Order> { firstOrder, secondOrder };
            var orderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            orderRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Order>()).Returns(orderRepo.Object);

            var orderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(orderItemRepo.Object);

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(r => r.Query()).Returns(new List<Table>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);

            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockMapper
                .Setup(m => m.Map<List<OrderItemDto>>(It.IsAny<List<OrderItem>>()))
                .Returns((List<OrderItem> items) =>
                    items
                        .Select(item => new OrderItemDto
                        {
                            OrderItemId = item.OrderItemId,
                            MenuItemId = item.MenuItemId,
                            ItemNameSnapshot = item.ItemNameSnapshot,
                            Quantity = item.Quantity,
                            PriceSnapshot = item.UnitPriceSnapshot,
                            ItemNote = item.ItemNote ?? string.Empty,
                            TotalPrice = item.GetTotalPrice(),
                        })
                        .ToList()
                );

            var handler = new MergeOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new MergeOrderCommand(firstOrderId, secondOrderId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            firstOrder.OrderItems.Should().HaveCount(2);
            firstOrder.OrderItems.Should().Contain(i => i.Status == OrderItemStatus.Completed);
            secondOrder.Status.Should().Be(OrderStatus.Merged);
        }

        private static Domain.Entities.Order CreateServingOrder(Guid orderId, string code, Guid tableId) =>
            new()
            {
                OrderId = orderId,
                OrderCode = code,
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = tableId,
                OrderItems = new List<OrderItem>(),
            };

        private static OrderItem CreateOrderItem(
            Guid orderId,
            Guid menuItemId,
            int quantity,
            decimal price,
            string name,
            OrderItemStatus status = OrderItemStatus.Cooking
        ) =>
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPriceSnapshot = price,
                Status = status,
                ItemCodeSnapshot = name.ToUpperInvariant(),
                ItemNameSnapshot = name,
                StationSnapshot = "Kitchen",
                OptionGroups = new List<OrderItemOptionGroup>(),
            };
    }
}

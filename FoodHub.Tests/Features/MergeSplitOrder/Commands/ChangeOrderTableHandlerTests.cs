using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.MergeSplitOrder.Commands
{
    public class ChangeOrderTableHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
        private readonly Mock<IMessageService> _mockMessageService = new();
        private readonly Mock<ILogger<ChangeOrderTableHandler>> _mockLogger = new();

        [Fact]
        public async Task Handle_Should_Move_Order_To_New_Table_And_Update_Table_Statuses()
        {
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var oldTableId = Guid.NewGuid();
            var newTableId = Guid.NewGuid();

            var order = new Order
            {
                OrderId = orderId,
                OrderCode = "ORD-001",
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = oldTableId,
            };

            var oldTable = new Table
            {
                TableId = oldTableId,
                TableNumber = 1,
                Status = TableStatus.Occupied,
                Orders = new List<Order> { order },
            };
            var newTable = new Table
            {
                TableId = newTableId,
                TableNumber = 2,
                Status = TableStatus.Available,
                Orders = new List<Order>(),
            };

            var orderRepo = new Mock<IGenericRepository<Order>>();
            orderRepo
                .Setup(r => r.Query())
                .Returns(new List<Order> { order }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Order>()).Returns(orderRepo.Object);

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo
                .Setup(r => r.Query())
                .Returns(new List<Table> { oldTable, newTable }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);

            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new ChangeOrderTableHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new ChangeOrderTableCommand(orderId, newTableId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            order.TableId.Should().Be(newTableId);
            oldTable.Status.Should().Be(TableStatus.Available);
            newTable.Status.Should().Be(TableStatus.Occupied);
            auditRepo.Verify(r => r.AddAsync(It.IsAny<OrderAuditLog>()), Times.Once);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_New_Table_Is_Not_Available()
        {
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var oldTableId = Guid.NewGuid();
            var newTableId = Guid.NewGuid();

            var order = new Order
            {
                OrderId = orderId,
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = oldTableId,
            };

            var tables = new List<Table>
            {
                new()
                {
                    TableId = oldTableId,
                    TableNumber = 1,
                    Status = TableStatus.Occupied,
                    Orders = new List<Order> { order },
                },
                new()
                {
                    TableId = newTableId,
                    TableNumber = 2,
                    Status = TableStatus.OutOfService,
                    Orders = new List<Order>(),
                },
            };

            var orderRepo = new Mock<IGenericRepository<Order>>();
            orderRepo
                .Setup(r => r.Query())
                .Returns(new List<Order> { order }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Order>()).Returns(orderRepo.Object);

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(r => r.Query()).Returns(tables.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Table.NotAvailable))
                .Returns("Table is not available");

            var handler = new ChangeOrderTableHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new ChangeOrderTableCommand(orderId, newTableId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }
    }
}

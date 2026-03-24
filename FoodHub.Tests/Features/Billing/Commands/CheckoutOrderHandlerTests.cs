using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Billing.Commands.CheckoutOrder;
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

namespace FoodHub.Tests.Features.Billing.Commands
{
    public class CheckoutOrderHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<ILogger<CheckoutOrderHandler>> _mockLogger = new();
        private readonly Mock<IMessageService> _mockMessageService = new();
        private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
        private readonly Mock<ICacheService> _mockCacheService = new();

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_DineInOrderCheckoutSucceeds()
        {
            var orderId = Guid.NewGuid();
            var tableId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new CheckoutOrderCommand
            {
                OrderId = orderId,
                PaymentMethod = PaymentMethod.Cash,
                AmountPaid = 200,
            };

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = orderId,
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = tableId,
                TotalAmount = 150,
                OrderItems = new List<OrderItem>(),
            };

            var table = new Table { TableId = tableId, Status = TableStatus.Occupied };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            var mockTableRepo = new Mock<IGenericRepository<Table>>();
            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order> { order }.AsQueryable().BuildMock());
            mockTableRepo
                .Setup(r => r.Query())
                .Returns(new List<Table> { table }.AsQueryable().BuildMock());

            _mockUow.Setup(u => u.Repository<FoodHub.Domain.Entities.Order>()).Returns(mockOrderRepo.Object);
            _mockUow.Setup(u => u.Repository<Table>()).Returns(mockTableRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var handler = new CheckoutOrderHandler(
                _mockUow.Object,
                _mockLogger.Object,
                _mockMessageService.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object,
                new Mock<ISignalRService>().Object
            );

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Paid);
            order.TableId.Should().BeNull();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderNotFound()
        {
            var command = new CheckoutOrderCommand
            {
                OrderId = Guid.NewGuid(),
                PaymentMethod = PaymentMethod.Cash,
                AmountPaid = 100,
            };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockOrderRepo.Setup(r => r.Query()).Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<FoodHub.Domain.Entities.Order>()).Returns(mockOrderRepo.Object);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid().ToString());
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.NotFound))
                .Returns("Order not found");

            var handler = new CheckoutOrderHandler(
                _mockUow.Object,
                _mockLogger.Object,
                _mockMessageService.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object,
                new Mock<ISignalRService>().Object
            );

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}

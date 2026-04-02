using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Billing.Commands.ProcessPaymentWebhook;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Domain.Entities;
using DomainOrder = FoodHub.Domain.Entities.Order;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Billing.Commands
{
    public class ProcessPaymentWebhookHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IPaymentService> _mockPayment;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ISignalRService> _mockSignalR;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<ProcessPaymentWebhookHandler>> _mockLogger;
        private readonly ProcessPaymentWebhookHandler _handler;

        public ProcessPaymentWebhookHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockPayment = new Mock<IPaymentService>();
            _mockCache = new Mock<ICacheService>();
            _mockSignalR = new Mock<ISignalRService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<ProcessPaymentWebhookHandler>>();

            _handler = new ProcessPaymentWebhookHandler(
                _mockUow.Object,
                _mockPayment.Object,
                _mockLogger.Object,
                _mockCache.Object,
                _mockSignalR.Object,
                _mockMessageService.Object);
        }

        [Fact]
        public async Task Handle_Should_UpdateOrderStatus_When_WebhookValid()
        {
            // Arrange
            long orderCode = 12345;
            var command = new ProcessPaymentWebhookCommand { WebhookBody = "{}" };

            _mockPayment.Setup(p => p.VerifyWebhookDataAsync(It.IsAny<string>())).ReturnsAsync(orderCode);
            _mockCache.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var orderId = Guid.NewGuid();
            var order = new DomainOrder { OrderId = orderId, TransactionCode = (int)orderCode, Status = OrderStatus.Serving, TotalAmount = 100000 };
            var tableId = Guid.NewGuid();
            order.TableId = tableId;
            order.OrderType = OrderType.DineIn;

            var orders = new List<DomainOrder> { order }.AsQueryable().BuildMock();
            var repoOrder = new Mock<IGenericRepository<DomainOrder>>();
            repoOrder.Setup(r => r.Query()).Returns(orders);
            _mockUow.Setup(u => u.Repository<DomainOrder>()).Returns(repoOrder.Object);

            var table = new Table { TableId = tableId, Status = TableStatus.Occupied };
            var repoTable = new Mock<IGenericRepository<Table>>();
            repoTable
                .Setup(r => r.Query())
                .Returns(new List<Table> { table }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(repoTable.Object);

            // Mock PaymentMethodConfig repo (needed for OrderPayment creation)
            var bankConfig = new PaymentMethodConfig
            {
                PaymentMethodConfigId = Guid.NewGuid(),
                Name = "Bank Transfer",
                Type = PaymentMethodType.BankTransfer,
                IsActive = true,
            };
            var repoPmc = new Mock<IGenericRepository<PaymentMethodConfig>>();
            repoPmc.Setup(r => r.Query())
                .Returns(new List<PaymentMethodConfig> { bankConfig }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<PaymentMethodConfig>()).Returns(repoPmc.Object);

            var repoOp = new Mock<IGenericRepository<OrderPayment>>();
            _mockUow.Setup(u => u.Repository<OrderPayment>()).Returns(repoOp.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Paid);
            table.Status.Should().Be(TableStatus.Available);
            order.TableId.Should().BeNull();
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _mockSignalR.Verify(s => s.NotifyOrderStatusChangedAsync(orderId, "Paid"), Times.Once);
            repoOp.Verify(r => r.AddAsync(It.IsAny<OrderPayment>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_SignatureInvalid()
        {
            // Arrange
            var command = new ProcessPaymentWebhookCommand { WebhookBody = "{}" };
            _mockPayment.Setup(p => p.VerifyWebhookDataAsync(It.IsAny<string>())).ThrowsAsync(new ArgumentException());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccessButDoNothing_When_LockExists()
        {
            // Arrange
            long orderCode = 12345;
            var command = new ProcessPaymentWebhookCommand { WebhookBody = "{}" };
            _mockPayment.Setup(p => p.VerifyWebhookDataAsync(It.IsAny<string>())).ReturnsAsync(orderCode);
            _mockCache.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }
    }
}

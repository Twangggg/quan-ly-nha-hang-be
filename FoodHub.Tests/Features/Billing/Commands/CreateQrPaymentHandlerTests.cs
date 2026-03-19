using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Billing.Commands.CreateQrPayment;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using DomainOrder = FoodHub.Domain.Entities.Order;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Billing.Commands
{
    public class CreateQrPaymentHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IPaymentService> _mockPayment;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockUser;
        private readonly Mock<ILogger<CreateQrPaymentHandler>> _mockLogger;
        private readonly CreateQrPaymentHandler _handler;

        public CreateQrPaymentHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockPayment = new Mock<IPaymentService>();
            _mockMessage = new Mock<IMessageService>();
            _mockUser = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<CreateQrPaymentHandler>>();

            _handler = new CreateQrPaymentHandler(
                _mockUow.Object,
                _mockPayment.Object,
                _mockLogger.Object,
                _mockMessage.Object,
                _mockUser.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_Valid()
        {
            // Arrange
            _mockUser.Setup(u => u.UserId).Returns(Guid.NewGuid().ToString());
            var orderId = Guid.NewGuid();
            var order = new DomainOrder { OrderId = orderId, Status = OrderStatus.Serving };
            
            var orders = new List<DomainOrder> { order }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<DomainOrder>>();
            repo.Setup(r => r.Query()).Returns(orders);
            _mockUow.Setup(u => u.Repository<DomainOrder>()).Returns(repo.Object);

            var payLink = new PaymentLinkResponse { CheckoutUrl = "https://payos.vn", QrCode = "QR" };
            _mockPayment.Setup(p => p.CreatePaymentLinkAsync(order, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payLink);

            var command = new CreateQrPaymentCommand { OrderId = orderId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.CheckoutUrl.Should().Be("https://payos.vn");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderNotFound()
        {
            // Arrange
            _mockUser.Setup(u => u.UserId).Returns(Guid.NewGuid().ToString());
            var orderId = Guid.NewGuid();
            
            var orders = new List<DomainOrder>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<DomainOrder>>();
            repo.Setup(r => r.Query()).Returns(orders);
            _mockUow.Setup(u => u.Repository<DomainOrder>()).Returns(repo.Object);

            _mockMessage.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("Not found");

            var command = new CreateQrPaymentCommand { OrderId = orderId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}

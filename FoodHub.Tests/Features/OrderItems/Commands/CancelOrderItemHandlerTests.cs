using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.OrderItems.Commands
{
    public class CancelOrderItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<CancelOrderItemHandler>> _mockLogger;
        private readonly CancelOrderItemHandler _handler;

        public CancelOrderItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<CancelOrderItemHandler>>();
            _handler = new CancelOrderItemHandler(
                _mockUow.Object,
                _mockMessageService.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                OrderId: Guid.NewGuid(),
                OrderItemId: orderItemId,
                Reason: "Customer requested",
                order: new Domain.Entities.Order()
            );

            _mockCurrentUserService.Setup(s => s.UserId).Returns(string.Empty);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Employee.CannotIdentifyUser))
                .Returns("Cannot identify user");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderItemNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                OrderId: orderId,
                OrderItemId: orderItemId,
                Reason: "Customer requested",
                order: new Domain.Entities.Order()
            );

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(new List<OrderItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.NotFound))
                .Returns("Order not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderItemStatusIsNotPreparing()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                OrderId: orderId,
                OrderItemId: orderItemId,
                Reason: "Customer requested",
                order: new Domain.Entities.Order()
            );

            var existingOrderItem = new OrderItem
            {
                OrderItemId = orderItemId,
                OrderId = orderId,
                Status = OrderItemStatus.Completed, // Not Preparing
                Quantity = 1,
                UnitPriceSnapshot = 10.00m,
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<OrderItem> { existingOrderItem }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.InvalidActionWithStatus))
                .Returns("Invalid action with current status");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderItemCancelled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                OrderId: orderId,
                OrderItemId: orderItemId,
                Reason: "Customer requested",
                order: new Domain.Entities.Order()
            );

            var existingOrderItem = new OrderItem
            {
                OrderItemId = orderItemId,
                OrderId = orderId,
                Status = OrderItemStatus.Preparing,
                Quantity = 2,
                UnitPriceSnapshot = 10.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                TotalAmount = 20.00m,
                OrderItems = new List<OrderItem> { existingOrderItem },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<OrderItem> { existingOrderItem }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<Domain.Entities.Order> { existingOrder }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            var mockAuditLogRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<OrderAuditLog>()));
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditLogRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingOrderItem.Status.Should().Be(OrderItemStatus.Cancelled);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

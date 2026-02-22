using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Orders.Commands.CancelOrder;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Order.Commands
{
    public class CancelOrderTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CancelOrderHandler>> _mockLogger;
        private readonly CancelOrderHandler _handler;

        public CancelOrderTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CancelOrderHandler>>();

            _handler = new CancelOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderCancelled_ForDineIn()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var tableId = Guid.NewGuid();
            var command = new CancelOrderCommand
            {
                OrderId = orderId,
                Status = OrderStatus.Cancelled,
                Reason = "Customer request",
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TableId = tableId,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { Status = OrderItemStatus.Preparing },
                    new OrderItem { Status = OrderItemStatus.Cooking },
                    new OrderItem { Status = OrderItemStatus.Ready },
                    new OrderItem { Status = OrderItemStatus.Completed }, // Should not be cancelled
                },
            };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Order> { order }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Cancelled);
            order.TableId.Should().BeNull(); // Table released for dine-in
            order
                .OrderItems.Where(oi => oi.Status == OrderItemStatus.Preparing)
                .All(oi => oi.Status == OrderItemStatus.Cancelled)
                .Should()
                .BeTrue();
            order
                .OrderItems.Where(oi => oi.Status == OrderItemStatus.Cooking)
                .All(oi => oi.Status == OrderItemStatus.Cancelled)
                .Should()
                .BeTrue();
            order
                .OrderItems.Where(oi => oi.Status == OrderItemStatus.Ready)
                .All(oi => oi.Status == OrderItemStatus.Cancelled)
                .Should()
                .BeTrue();
            order
                .OrderItems.Where(oi => oi.Status == OrderItemStatus.Completed)
                .All(oi => oi.Status != OrderItemStatus.Cancelled)
                .Should()
                .BeTrue();
            _mockUow.Verify(
                u => u.Repository<OrderAuditLog>().AddAsync(It.IsAny<OrderAuditLog>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderCancelled_ForTakeaway()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new CancelOrderCommand
            {
                OrderId = orderId,
                Status = OrderStatus.Cancelled,
                Reason = "Out of stock",
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.Takeaway,
                TableId = null,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { Status = OrderItemStatus.Preparing },
                },
            };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Order> { order }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Cancelled);
            order.TableId.Should().BeNull(); // No table for takeaway
            order.OrderItems.First().Status.Should().Be(OrderItemStatus.Cancelled);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var command = new CancelOrderCommand
            {
                OrderId = orderId,
                Status = OrderStatus.Cancelled,
                Reason = "Test",
            };

            var userId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.NotFound))
                .Returns("Order not found.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            var command = new CancelOrderCommand
            {
                OrderId = Guid.NewGuid(),
                Status = OrderStatus.Cancelled,
                Reason = "Test",
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns((string)null);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.UserNotLoggedIn))
                .Returns("User not logged in.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
            _mockUow.Verify(
                u => u.Repository<FoodHub.Domain.Entities.Order>().Query(),
                Times.Never
            );
        }

        [Fact]
        public void Validator_ShouldHaveError_When_OrderIdIsEmpty()
        {
            var validator = new CancelOrderValidator();
            var command = new CancelOrderCommand
            {
                OrderId = Guid.Empty,
                Status = OrderStatus.Cancelled,
                Reason = "Test",
            };
            var result = validator.Validate(command);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "OrderId");
        }

        [Fact]
        public void Validator_ShouldHaveError_When_StatusIsEmpty()
        {
            var validator = new CancelOrderValidator();
            var command = new CancelOrderCommand
            {
                OrderId = Guid.NewGuid(),
                Status = default(OrderStatus),
                Reason = "Test",
            };
            var result = validator.Validate(command);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Status");
        }

        [Fact]
        public void Validator_ShouldHaveError_When_ReasonIsEmpty()
        {
            var validator = new CancelOrderValidator();
            var command = new CancelOrderCommand
            {
                OrderId = Guid.NewGuid(),
                Status = OrderStatus.Cancelled,
                Reason = "",
            };
            var result = validator.Validate(command);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
        }

        [Fact]
        public void Validator_ShouldNotHaveError_When_AllFieldsValid()
        {
            var validator = new CancelOrderValidator();
            var command = new CancelOrderCommand
            {
                OrderId = Guid.NewGuid(),
                Status = OrderStatus.Cancelled,
                Reason = "Valid reason",
            };
            var result = validator.Validate(command);
            Assert.True(result.IsValid);
        }
    }
}

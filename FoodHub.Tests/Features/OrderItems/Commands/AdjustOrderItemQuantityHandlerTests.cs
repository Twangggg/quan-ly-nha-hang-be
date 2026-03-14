using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.OrderItems.Commands.AdjustOrderItemQuantity;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.OrderItems.Commands
{
    public class AdjustOrderItemQuantityHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AdjustOrderItemQuantityHandler>> _mockLogger;
        private readonly AdjustOrderItemQuantityHandler _handler;

        public AdjustOrderItemQuantityHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<AdjustOrderItemQuantityHandler>>();
            _handler = new AdjustOrderItemQuantityHandler(
                _mockUow.Object,
                _mockMessageService.Object,
                _mockCurrentUserService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            var command = new AdjustOrderItemQuantityCommand { OrderId = Guid.NewGuid(), OrderItemId = Guid.NewGuid(), Quantity = 2 };
            _mockCurrentUserService.Setup(s => s.UserId).Returns(string.Empty);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.UserNotLoggedIn)).Returns("User not logged in");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderNotFound()
        {
            var command = new AdjustOrderItemQuantityCommand { OrderId = Guid.NewGuid(), OrderItemId = Guid.NewGuid(), Quantity = 2 };
            _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid().ToString());

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo.Setup(r => r.Query()).Returns(new List<Domain.Entities.Order>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Order>()).Returns(mockOrderRepo.Object);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Order.NotFound)).Returns("Order not found");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderItemNotFound()
        {
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var command = new AdjustOrderItemQuantityCommand { OrderId = orderId, OrderItemId = Guid.NewGuid(), Quantity = 2 };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderItems = new List<OrderItem>()
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo.Setup(r => r.Query()).Returns(new List<Domain.Entities.Order> { existingOrder }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Order>()).Returns(mockOrderRepo.Object);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.OrderItem.NotFound)).Returns("Item not found");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_QuantityLessThanOne()
        {
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new AdjustOrderItemQuantityCommand { OrderId = orderId, OrderItemId = orderItemId, Quantity = 0 };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { OrderItemId = orderItemId, Status = OrderItemStatus.Preparing }
                }
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo.Setup(r => r.Query()).Returns(new List<Domain.Entities.Order> { existingOrder }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Order>()).Returns(mockOrderRepo.Object);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.OrderItem.InvalidQuantity)).Returns("Invalid quantity");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_ValidCommand()
        {
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new AdjustOrderItemQuantityCommand { OrderId = orderId, OrderItemId = orderItemId, Quantity = 5, Reason = "Customer asked" };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { OrderItemId = orderItemId, Status = OrderItemStatus.Preparing, Quantity = 2, UnitPriceSnapshot = 10 }
                }
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());
            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo.Setup(r => r.Query()).Returns(new List<Domain.Entities.Order> { existingOrder }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Order>()).Returns(mockOrderRepo.Object);

            var mockAuditLogRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditLogRepo.Object);

            _mockMapper.Setup(m => m.Map<AdjustOrderItemQuantityResponse>(It.IsAny<Domain.Entities.Order>())).Returns(new AdjustOrderItemQuantityResponse());

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            existingOrder.OrderItems.First().Quantity.Should().Be(5);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

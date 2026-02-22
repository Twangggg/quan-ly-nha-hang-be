using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Orders.Commands.CompleteOrder;
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
    public class CompleteOrderTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<CompleteOrderHandler>> _mockLogger;
        private readonly CompleteOrderHandler _handler;

        public CompleteOrderTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<CompleteOrderHandler>>();

            _handler = new CompleteOrderHandler(
                _mockUow.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderCompleted_ForDineIn_AllItemsFinished()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new CompleteOrderCommand { OrderId = orderId };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TableId = Guid.NewGuid(),
                TotalAmount = 0,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Quantity = 2,
                        UnitPriceSnapshot = 100,
                        Status = OrderItemStatus.Completed,
                        OptionGroups = new List<OrderItemOptionGroup>
                        {
                            new OrderItemOptionGroup
                            {
                                GroupNameSnapshot = "Test Group",
                                GroupTypeSnapshot = "Required",
                                IsRequiredSnapshot = true,
                                OptionValues = new List<OrderItemOptionValue>
                                {
                                    new OrderItemOptionValue
                                    {
                                        LabelSnapshot = "Test Label",
                                        ExtraPriceSnapshot = 10,
                                        Quantity = 1,
                                    },
                                },
                            },
                        },
                    },
                },
            };

            var expectedTotal = (2 * 100) + ((10 * 1) * 2); // 200 + 20 = 220

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

            var response = new CompleteOrderResponse { OrderId = orderId };
            _mockMapper.Setup(m => m.Map<CompleteOrderResponse>(order)).Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(response);
            order.Status.Should().Be(OrderStatus.Completed);
            order.TotalAmount.Should().Be(expectedTotal);
            order.TableId.Should().BeNull(); // Table released for dine-in when all items finished
            order.CompletedAt.Should().NotBeNull();
            order.UpdatedAt.Should().NotBeNull();
            _mockUow.Verify(
                u => u.Repository<OrderAuditLog>().AddAsync(It.IsAny<OrderAuditLog>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderCompleted_ForTakeaway()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new CompleteOrderCommand { OrderId = orderId };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.Takeaway,
                TableId = null,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Quantity = 1,
                        UnitPriceSnapshot = 50,
                        Status = OrderItemStatus.Completed,
                        OptionGroups = new List<OrderItemOptionGroup>(),
                    },
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

            var response = new CompleteOrderResponse { OrderId = orderId };
            _mockMapper.Setup(m => m.Map<CompleteOrderResponse>(order)).Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Completed);
            order.TotalAmount.Should().Be(50);
            order.TableId.Should().BeNull(); // No table for takeaway
            order.CompletedAt.Should().NotBeNull();
            order.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var command = new CompleteOrderCommand { OrderId = orderId };

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
            var command = new CompleteOrderCommand { OrderId = Guid.NewGuid() };

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
        public async Task Handle_Should_CalculateTotalAmount_ExcludingCancelledAndRejectedItems()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var command = new CompleteOrderCommand { OrderId = orderId };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.Takeaway,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Quantity = 1,
                        UnitPriceSnapshot = 100,
                        Status = OrderItemStatus.Completed,
                        OptionGroups = new List<OrderItemOptionGroup>(),
                    },
                    new OrderItem
                    {
                        Quantity = 1,
                        UnitPriceSnapshot = 50,
                        Status = OrderItemStatus.Cancelled, // Should be excluded
                        OptionGroups = new List<OrderItemOptionGroup>(),
                    },
                    new OrderItem
                    {
                        Quantity = 1,
                        UnitPriceSnapshot = 75,
                        Status = OrderItemStatus.Rejected, // Should be excluded
                        OptionGroups = new List<OrderItemOptionGroup>(),
                    },
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

            var response = new CompleteOrderResponse { OrderId = orderId };
            _mockMapper.Setup(m => m.Map<CompleteOrderResponse>(order)).Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.TotalAmount.Should().Be(100); // Only completed item
        }
    }
}

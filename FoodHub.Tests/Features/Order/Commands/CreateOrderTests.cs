using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Orders.Commands.CreateOrder;
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
    public class CreateOrderTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<CreateOrderHandler>> _mockLogger;
        private readonly CreateOrderHandler _handler;

        public CreateOrderTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CreateOrderHandler>>();

            _handler = new CreateOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderCreated_ForTakeaway()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = "Test note",
            };

            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Mock query for order code generation (no existing orders)
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderCreated_ForDineIn_WithTableId()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                OrderType = OrderType.DineIn,
                TableId = tableId,
                Note = null,
            };

            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Mock query for order code generation
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(
                            It.Is<FoodHub.Domain.Entities.Order>(o =>
                                o.OrderType == OrderType.DineIn
                                && o.TableId == tableId
                                && o.Status == OrderStatus.Serving
                            )
                        ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DineIn_WithoutTableId()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                OrderType = OrderType.DineIn,
                TableId = null,
                Note = "Test",
            };

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.SelectTable))
                .Returns("Please select a table for dine-in orders.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_GenerateCorrectOrderCode_When_NoExistingOrders()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = null,
            };

            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Mock query for order code generation (no existing orders)
            mockRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(
                            It.Is<FoodHub.Domain.Entities.Order>(o =>
                                o.OrderCode.StartsWith("ORD-") && o.OrderCode.EndsWith("-0001")
                            )
                        ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_GenerateCorrectOrderCode_When_ExistingOrdersPresent()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = null,
            };

            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var existingOrder = new FoodHub.Domain.Entities.Order
            {
                OrderCode = $"ORD-{today}-0005",
            };

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Mock query for order code generation
            mockRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Order> { existingOrder }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(
                            It.Is<FoodHub.Domain.Entities.Order>(o =>
                                o.OrderCode == $"ORD-{today}-0006"
                            )
                        ),
                Times.Once
            );
        }
    }
}

using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Orders.Commands.CreateOrder;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;
using ReservationEntity = FoodHub.Domain.Entities.Reservation;

namespace FoodHub.Tests.Features.Order.Commands
{
    public class CreateOrderTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ISignalRService> _mockSignalRService;
        private readonly Mock<ILogger<CreateOrderHandler>> _mockLogger;
        private readonly CreateOrderHandler _handler;

        public CreateOrderTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockSignalRService = new Mock<ISignalRService>();
            _mockLogger = new Mock<ILogger<CreateOrderHandler>>();

            _handler = new CreateOrderHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object,
                _mockSignalRService.Object,
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
                ReservationId = null,
                Note = "Test note",
            };

            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

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
        public async Task Handle_Should_ReturnSuccess_When_OrderCreated_ForDineIn_WithReservationId()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var tableId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                OrderType = OrderType.DineIn,
                ReservationId = reservationId,
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

            var area = new Area
            {
                AreaId = Guid.NewGuid(),
                Name = "Standard",
                CodePrefix = "STD",
                Type = AreaType.Normal,
                Status = AreaStatus.Active,
            };
            var table = new Table
            {
                TableId = tableId,
                TableNumber = 1,
                Capacity = 4,
                Status = TableStatus.Available,
                Area = area,
                AreaId = area.AreaId,
            };

            var reservation = new ReservationEntity
            {
                ReservationId = reservationId,
                TableId = tableId,
                Table = table,
                Status = ReservationStatus.Booked
            };

            var reservationRepo = new Mock<IGenericRepository<ReservationEntity>>();
            reservationRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<ReservationEntity> { reservation }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<ReservationEntity>()).Returns(reservationRepo.Object);
            
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(r => r.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);

            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

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
                                && o.ReservationId == reservationId
                                && o.Status == OrderStatus.Serving
                            )
                        ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DineIn_WithoutReservationId()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                OrderType = OrderType.DineIn,
                ReservationId = null,
                Note = "Test",
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid().ToString());
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
                ReservationId = null,
                Note = null,
            };

            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

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
                ReservationId = null,
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

            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

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

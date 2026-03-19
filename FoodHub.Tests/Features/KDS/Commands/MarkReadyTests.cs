using FluentAssertions;
using FoodHub.Application.Features.KDS.Commands.MarkReady;
using FoodHub.Application.Features.KDS.Common;
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

namespace FoodHub.Tests.Features.KDS.Commands
{
    public class MarkReadyTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ISignalRService> _mockSignalRService;
        private readonly Mock<IInventoryDeductionService> _mockInventoryDeductionService;
        private readonly Mock<ILogger<MarkReadyHandler>> _mockLogger;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly MarkReadyHandler _handler;

        public MarkReadyTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockSignalRService = new Mock<ISignalRService>();
            _mockInventoryDeductionService = new Mock<IInventoryDeductionService>();
            _mockLogger = new Mock<ILogger<MarkReadyHandler>>();
            _priorityCalculator = new KdsPriorityCalculator(); // Dùng instance thật vì logic tính điểm đơn giản

            _handler = new MarkReadyHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockSignalRService.Object,
                _priorityCalculator,
                _mockInventoryDeductionService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_TriggerAutoPull_WithPriorityItem()
        {
            // Arrange
            var orderItemId = Guid.NewGuid();
            var station = "HotKitchen";
            var userId = Guid.NewGuid().ToString();
            var menuItem = new MenuItem { ExpectedTime = 10, Code = "TEST001", Name = "Test Item", ImageUrl = "http://test.com/image.jpg" };

            var currentItem = new OrderItem
            {
                OrderItemId = orderItemId,
                StationSnapshot = station,
                Status = OrderItemStatus.Cooking,
                OrderId = Guid.NewGuid(),
                MenuItem = menuItem,
            };

            var normalItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                StationSnapshot = station,
                Status = OrderItemStatus.Preparing,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                Order = new FoodHub.Domain.Entities.Order { IsPriority = false },
                MenuItem = menuItem,
            };

            var vipItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                StationSnapshot = station,
                Status = OrderItemStatus.Preparing,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                Order = new FoodHub.Domain.Entities.Order { IsPriority = true },
                MenuItem = menuItem,
            };

            var items = new List<OrderItem> { currentItem, normalItem, vipItem };
            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo.Setup(r => r.Query()).Returns(items.AsQueryable().BuildMock());

            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();

            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            // Act
            var result = await _handler.Handle(
                new MarkReadyCommand { OrderItemId = orderItemId },
                CancellationToken.None
            );

            // Assert
            result.IsSuccess.Should().BeTrue();
            currentItem.Status.Should().Be(OrderItemStatus.Ready);

            // KIỂM TRA QUAN TRỌNG: Món VIP phải được chọn để nấu (Auto-pull) thay vì món thường chờ lâu hơn
            vipItem.Status.Should().Be(OrderItemStatus.Cooking);
            normalItem.Status.Should().Be(OrderItemStatus.Preparing);

            _mockSignalRService.Verify(
                s =>
                    s.NotifyOrderItemStatusChangedAsync(
                        vipItem.OrderItemId,
                        OrderItemStatus.Cooking,
                        station
                    ),
                Times.Once
            );
        }
    }
}

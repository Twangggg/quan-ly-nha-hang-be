using FluentAssertions;
using FoodHub.Application.Features.KDS.Commands.RejectOrderItem;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.KDS.Commands
{
    public class RejectOrderItemTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IKdsSettingsProvider> _mockKdsSettingsProvider;
        private readonly Mock<ISignalRService> _mockSignalRService;
        private readonly Mock<IKdsAutoPullService> _mockKdsAutoPullService;
        private readonly Mock<ILogger<RejectOrderItemHandler>> _mockLogger;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly RejectOrderItemHandler _handler;

        public RejectOrderItemTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockKdsSettingsProvider = new Mock<IKdsSettingsProvider>();
            _mockSignalRService = new Mock<ISignalRService>();
            _mockKdsAutoPullService = new Mock<IKdsAutoPullService>();
            _mockLogger = new Mock<ILogger<RejectOrderItemHandler>>();
            _priorityCalculator = new KdsPriorityCalculator();

            _mockKdsSettingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(KdsSettings.CreateDefault());

            _handler = new RejectOrderItemHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockSignalRService.Object,
                _priorityCalculator,
                _mockKdsSettingsProvider.Object,
                _mockKdsAutoPullService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_RejectItem_And_TriggerAutoPull()
        {
            var orderItemId = Guid.NewGuid();
            var station = "ColdKitchen";
            var userId = Guid.NewGuid().ToString();
            var reason = "Hết nguyên liệu";
            var menuItem = new MenuItem
            {
                ExpectedTime = 10,
                Code = "TEST001",
                Name = "Test Item",
                ImageUrl = "http://test.com/image.jpg",
            };

            var currentItem = new OrderItem
            {
                OrderItemId = orderItemId,
                StationSnapshot = station,
                Status = OrderItemStatus.Cooking,
                OrderId = Guid.NewGuid(),
                MenuItem = menuItem,
            };

            var nextItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                StationSnapshot = station,
                Status = OrderItemStatus.Preparing,
                Order = new FoodHub.Domain.Entities.Order { IsPriority = false },
                MenuItem = menuItem,
                CreatedAt = DateTime.UtcNow,
            };

            var items = new List<OrderItem> { currentItem, nextItem };
            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo.Setup(r => r.Query()).Returns(items.AsQueryable().BuildMock());

            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();

            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var result = await _handler.Handle(
                new RejectOrderItemCommand { OrderItemId = orderItemId, Reason = reason },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            currentItem.Status.Should().Be(OrderItemStatus.Rejected);
            currentItem.RejectionReason.Should().Be(reason);
            nextItem.Status.Should().Be(OrderItemStatus.Cooking);
        }
    }
}

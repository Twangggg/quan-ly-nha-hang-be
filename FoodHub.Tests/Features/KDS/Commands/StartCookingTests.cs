using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.KDS.Commands.StartCooking;
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
    public class StartCookingTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IKdsSettingsProvider> _mockKdsSettingsProvider;
        private readonly Mock<ISignalRService> _mockSignalRService;
        private readonly Mock<ILogger<StartCookingHandler>> _mockLogger;
        private readonly StartCookingHandler _handler;

        public StartCookingTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockKdsSettingsProvider = new Mock<IKdsSettingsProvider>();
            _mockSignalRService = new Mock<ISignalRService>();
            _mockLogger = new Mock<ILogger<StartCookingHandler>>();

            _mockKdsSettingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(KdsSettings.CreateDefault());

            _handler = new StartCookingHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockKdsSettingsProvider.Object,
                _mockSignalRService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_WipLimitExceeded()
        {
            var orderItemId = Guid.NewGuid();
            var station = "HotKitchen";

            _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid().ToString());

            var orderItem = new OrderItem
            {
                OrderItemId = orderItemId,
                StationSnapshot = station,
                Status = OrderItemStatus.Preparing,
            };

            var cookingItems = new List<OrderItem>
            {
                new() { Status = OrderItemStatus.Cooking, StationSnapshot = station },
                new() { Status = OrderItemStatus.Cooking, StationSnapshot = station },
                new() { Status = OrderItemStatus.Cooking, StationSnapshot = station },
                new() { Status = OrderItemStatus.Cooking, StationSnapshot = station },
            };

            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(cookingItems.Concat(new[] { orderItem }).AsQueryable().BuildMock());

            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.KDS.WipLimitExceeded))
                .Returns("WIP limit exceeded");

            var result = await _handler.Handle(
                new StartCookingCommand { OrderItemId = orderItemId },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("WIP limit exceeded");
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Succeed_When_WipLimitNotReached()
        {
            var orderItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var station = "HotKitchen";

            var orderItem = new OrderItem
            {
                OrderItemId = orderItemId,
                StationSnapshot = station,
                Status = OrderItemStatus.Preparing,
                OrderId = Guid.NewGuid(),
            };

            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo.Setup(r => r.Query()).Returns(new List<OrderItem> { orderItem }.AsQueryable().BuildMock());

            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();

            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var result = await _handler.Handle(
                new StartCookingCommand { OrderItemId = orderItemId },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            orderItem.Status.Should().Be(OrderItemStatus.Cooking);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _mockSignalRService.Verify(
                s => s.NotifyOrderItemStatusChangedAsync(orderItemId, OrderItemStatus.Cooking, station),
                Times.Once
            );
        }
    }
}

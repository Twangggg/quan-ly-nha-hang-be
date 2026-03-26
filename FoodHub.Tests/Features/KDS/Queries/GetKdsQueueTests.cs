using FluentAssertions;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Features.KDS.Queries.GetKdsQueue;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.KDS.Queries
{
    public class GetKdsQueueTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IKdsSettingsProvider> _mockKdsSettingsProvider;
        private readonly Mock<ILogger<GetKdsQueueHandler>> _mockLogger;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly GetKdsQueueHandler _handler;

        public GetKdsQueueTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockKdsSettingsProvider = new Mock<IKdsSettingsProvider>();
            _mockLogger = new Mock<ILogger<GetKdsQueueHandler>>();
            _priorityCalculator = new KdsPriorityCalculator();

            _mockKdsSettingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(KdsSettings.CreateDefault());

            _handler = new GetKdsQueueHandler(
                _mockUow.Object,
                _priorityCalculator,
                _mockKdsSettingsProvider.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnQueue_WithCorrectPositions()
        {
            var station = "HotKitchen";
            var vipOrder = new FoodHub.Domain.Entities.Order { IsPriority = true };
            var normalOrder = new FoodHub.Domain.Entities.Order { IsPriority = false };
            var menuItem = new MenuItem
            {
                ExpectedTime = 10,
                Code = "TEST001",
                Name = "Test Item",
                ImageUrl = "http://test.com/image.jpg",
            };

            var normalItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Preparing,
                StationSnapshot = station,
                Order = normalOrder,
                MenuItem = menuItem,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            };

            var vipItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Preparing,
                StationSnapshot = station,
                Order = vipOrder,
                MenuItem = menuItem,
                CreatedAt = DateTime.UtcNow,
            };

            var items = new List<OrderItem> { normalItem, vipItem };

            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo.Setup(r => r.Query()).Returns(items.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);

            var result = await _handler.Handle(
                new GetKdsQueueQuery { Station = station },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            var data = result.Data!;

            data[0].OrderItemId.Should().Be(vipItem.OrderItemId);
            data[0].QueuePosition.Should().Be(1);

            data[1].OrderItemId.Should().Be(normalItem.OrderItemId);
            data[1].QueuePosition.Should().Be(2);
        }
    }
}

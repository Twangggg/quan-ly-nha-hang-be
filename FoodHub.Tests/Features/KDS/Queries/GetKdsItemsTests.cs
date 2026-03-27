using FluentAssertions;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Features.KDS.Queries.GetKdsItems;
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
    public class GetKdsItemsTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IKdsSettingsProvider> _mockKdsSettingsProvider;
        private readonly Mock<ILogger<GetKdsItemsHandler>> _mockLogger;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly GetKdsItemsHandler _handler;

        public GetKdsItemsTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockKdsSettingsProvider = new Mock<IKdsSettingsProvider>();
            _mockLogger = new Mock<ILogger<GetKdsItemsHandler>>();
            _priorityCalculator = new KdsPriorityCalculator();

            _mockKdsSettingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(KdsSettings.CreateDefault());

            _handler = new GetKdsItemsHandler(
                _mockUow.Object,
                _priorityCalculator,
                _mockKdsSettingsProvider.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnItems_SortedByCookingThenPriority()
        {
            var station = "Bar";
            var order = new FoodHub.Domain.Entities.Order { IsPriority = false };
            var vipOrder = new FoodHub.Domain.Entities.Order { IsPriority = true };

            var menuItem = new MenuItem
            {
                ExpectedTime = 10,
                Code = "TEST001",
                Name = "Test Item",
                ImageUrl = "http://test.com/image.jpg",
            };

            var cookingItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Cooking,
                StationSnapshot = station,
                Order = order,
                MenuItem = menuItem,
                CreatedAt = DateTime.UtcNow,
            };

            var preparingNormal = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Preparing,
                StationSnapshot = station,
                Order = order,
                MenuItem = menuItem,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            };

            var preparingVip = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Preparing,
                StationSnapshot = station,
                Order = vipOrder,
                MenuItem = menuItem,
                CreatedAt = DateTime.UtcNow,
            };

            var items = new List<OrderItem> { preparingNormal, cookingItem, preparingVip };

            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo.Setup(r => r.Query()).Returns(items.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);

            var result = await _handler.Handle(
                new GetKdsItemsQuery { Station = station },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            var data = result.Data!;

            data[0].OrderItemId.Should().Be(cookingItem.OrderItemId);
            data[1].OrderItemId.Should().Be(preparingVip.OrderItemId);
            data[2].OrderItemId.Should().Be(preparingNormal.OrderItemId);
        }
    }
}

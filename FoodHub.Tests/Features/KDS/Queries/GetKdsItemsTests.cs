using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Features.KDS.Queries.GetKdsItems;
using FoodHub.Application.Interfaces;
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
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<GetKdsItemsHandler>> _mockLogger;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly GetKdsItemsHandler _handler;

        public GetKdsItemsTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<GetKdsItemsHandler>>();
            _priorityCalculator = new KdsPriorityCalculator();

            _handler = new GetKdsItemsHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _priorityCalculator,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnItems_SortedByCookingThenPriority()
        {
            // Arrange
            var station = "Bar";
            var order = new FoodHub.Domain.Entities.Order { IsPriority = false };
            var vipOrder = new FoodHub.Domain.Entities.Order { IsPriority = true };

            var cookingItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Cooking,
                StationSnapshot = station,
                Order = order,
                CreatedAt = DateTime.UtcNow,
            };

            var preparingNormal = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Preparing,
                StationSnapshot = station,
                Order = order,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            };

            var preparingVip = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                Status = OrderItemStatus.Preparing,
                StationSnapshot = station,
                Order = vipOrder,
                CreatedAt = DateTime.UtcNow, // Mới tạo nhưng là VIP
            };

            var items = new List<OrderItem> { preparingNormal, cookingItem, preparingVip };

            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo.Setup(r => r.Query()).Returns(items.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);

            // Mock Mapper chuyển đổi sang Response
            _mockMapper
                .Setup(m => m.Map<List<KdsItemResponse>>(It.IsAny<List<OrderItem>>()))
                .Returns(
                    (List<OrderItem> src) =>
                        src.Select(s => new KdsItemResponse
                            {
                                OrderItemId = s.OrderItemId,
                                Status = s.Status.ToString(),
                                CreatedAt = s.CreatedAt,
                            })
                            .ToList()
                );

            // Act
            var result = await _handler.Handle(
                new GetKdsItemsQuery { Station = station },
                CancellationToken.None
            );

            // Assert
            result.IsSuccess.Should().BeTrue();
            var data = result.Data!;

            // Thứ tự kỳ vọng:
            // 1. Cooking
            // 2. VIP (điểm cao nhất do +50)
            // 3. Normal
            data[0].OrderItemId.Should().Be(cookingItem.OrderItemId);
            data[1].OrderItemId.Should().Be(preparingVip.OrderItemId);
            data[2].OrderItemId.Should().Be(preparingNormal.OrderItemId);
        }
    }
}

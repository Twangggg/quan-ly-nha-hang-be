using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Orders.Queries.GetOrderById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Orders
{
    public class GetOrderByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<GetOrderByIdHandler>> _mockLogger;
        private readonly GetOrderByIdHandler _handler;

        public GetOrderByIdHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<GetOrderByIdHandler>>();

            _handler = new GetOrderByIdHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockMessageService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = orderId,
                OrderCode = "ORD-20231027-001",
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TotalAmount = 100000,
            };

            var orders = new List<FoodHub.Domain.Entities.Order> { order }
                .AsQueryable()
                .BuildMock();

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockRepo.Setup(r => r.Query()).Returns(orders);
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);

            var query = new GetOrderByIdQuery { OrderId = orderId };

            _mockMapper
                .Setup(m => m.Map<GetOrderByIdResponse>(order))
                .Returns(new GetOrderByIdResponse { OrderId = orderId });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.OrderId.Should().Be(orderId);

            _mockLogger.Verify(
                x =>
                    x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>(
                            (v, t) =>
                                v.ToString().Contains($"Successfully retrieved order {orderId}")
                        ),
                        It.IsAny<Exception?>(),
                        It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_OrderDoesNotExist()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var orders = new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock();

            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockRepo.Setup(r => r.Query()).Returns(orders);
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);

            var query = new GetOrderByIdQuery { OrderId = orderId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain($"Order with ID {orderId} was not found");
        }
    }
}

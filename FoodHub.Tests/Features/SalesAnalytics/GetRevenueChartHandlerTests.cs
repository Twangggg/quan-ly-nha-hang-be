using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetRevenueChart;
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

namespace FoodHub.Tests.Features.SalesAnalytics
{
    public class GetRevenueChartHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<GetRevenueChartHandler>> _mockLogger;
        private readonly GetRevenueChartHandler _handler;
        private static readonly TimeZoneInfo _vnTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        public GetRevenueChartHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GetRevenueChartHandler>>();
            _handler = new GetRevenueChartHandler(_mockUow.Object, _mockLogger.Object);
        }

        private void SetupOrderRepo(IEnumerable<FoodHub.Domain.Entities.Order> orders)
        {
            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
        }

        [Fact]
        public async Task Handle_WithDate_ShouldReturnHourlyBreakdown()
        {
            // Arrange
            var date = new DateOnly(2026, 3, 10);
            var dtVn = date.ToDateTime(new TimeOnly(10, 0));
            var dtUtc = TimeZoneInfo.ConvertTimeToUtc(dtVn, _vnTz);

            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    TotalAmount = 500000,
                    Status = OrderStatus.Paid,
                    PaidAt = dtUtc,
                },
            };
            SetupOrderRepo(orders);

            var query = new GetRevenueChartQuery { Date = date };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Points.Should().HaveCount(24);
            result.Data.Points.First(p => p.Label == "10:00").Revenue.Should().Be(500000);
            result.Data.Points.Where(p => p.Label != "10:00").Sum(p => p.Revenue).Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithMonth_ShouldReturnDailyBreakdown()
        {
            // Arrange
            var year = 2026;
            var month = 3;
            var dtVn = new DateTime(year, month, 15, 12, 0, 0);
            var dtUtc = TimeZoneInfo.ConvertTimeToUtc(dtVn, _vnTz);

            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    TotalAmount = 1000000,
                    Status = OrderStatus.Completed,
                    PaidAt = dtUtc,
                },
            };
            SetupOrderRepo(orders);

            var query = new GetRevenueChartQuery { Year = year, Month = month };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Points.Should().HaveCount(31); // March has 31 days
            result.Data.Points.First(p => p.Label == "15/03").Revenue.Should().Be(1000000);
        }

        [Fact]
        public async Task Handle_WithNoOrders_ShouldReturnZeroedPoints()
        {
            // Arrange
            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order>());
            var query = new GetRevenueChartQuery { Date = new DateOnly(2026, 3, 10) };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Points.Should().HaveCount(24);
            result.Data.Points.Sum(p => p.Revenue).Should().Be(0);
        }
    }
}

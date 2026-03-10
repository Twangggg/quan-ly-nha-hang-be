using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SalesAnalytics
{
    public class GetDailyReportHandlerTests
    {
        private static readonly TimeZoneInfo VnTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<GetDailyReportHandler>> _mockLogger;
        private readonly GetDailyReportHandler _handler;

        public GetDailyReportHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GetDailyReportHandler>>();
            _handler = new GetDailyReportHandler(_mockUow.Object, _mockLogger.Object);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// Tạo DateTime UTC tương ứng với giờ VN (HH:mm ngày d).
        private static DateTime VnToUtc(DateOnly d, int hour = 0, int minute = 0) =>
            TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(new TimeOnly(hour, minute)), VnTz);

        private void SetupOrderRepo(IEnumerable<FoodHub.Domain.Entities.Order> orders)
        {
            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Handle_Should_Return_TotalRevenue_And_OrderCount_ForDate()
        {
            // Arrange
            var reportDate = new DateOnly(2026, 3, 10);

            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 200_000,
                    PaidAt = VnToUtc(reportDate, 9),
                },
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Completed,
                    TotalAmount = 350_000,
                    PaidAt = VnToUtc(reportDate, 14),
                },
                // Order from previous day - should NOT be counted
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 100_000,
                    PaidAt = VnToUtc(reportDate.AddDays(-1), 20),
                },
            };
            SetupOrderRepo(orders);

            var query = new GetDailyReportQuery { Date = reportDate, MovingAverageDays = 0 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalRevenue.Should().Be(550_000);
            result.Data.TotalOrders.Should().Be(2);
            result.Data.Date.Should().Be(reportDate);
        }

        [Fact]
        public async Task Handle_Should_Count_CancelledOrders_Separately()
        {
            // Arrange
            var reportDate = new DateOnly(2026, 3, 10);

            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 100_000,
                    PaidAt = VnToUtc(reportDate, 10),
                },
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Cancelled,
                    TotalAmount = 0,
                    PaidAt = VnToUtc(reportDate, 11),
                },
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Cancelled,
                    TotalAmount = 0,
                    PaidAt = VnToUtc(reportDate, 12),
                },
            };
            SetupOrderRepo(orders);

            var query = new GetDailyReportQuery { Date = reportDate, MovingAverageDays = 0 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.TotalOrders.Should().Be(1);
            result.Data.CancelledOrders.Should().Be(2);
            result.Data.TotalRevenue.Should().Be(100_000);
        }

        [Fact]
        public async Task Handle_Should_Return_Null_Target_When_NoHistoricalData()
        {
            // Arrange
            var reportDate = new DateOnly(2026, 3, 10);
            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order>());

            var query = new GetDailyReportQuery { Date = reportDate, MovingAverageDays = 7 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.DailyTarget.Should().BeNull();
            result.Data.AchievementRate.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Calculate_MovingAverage_Correctly()
        {
            // Arrange
            var reportDate = new DateOnly(2026, 3, 10);

            // 3 ngày trước có doanh thu: 100k, 200k, 300k → avg = 200k
            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 100_000,
                    PaidAt = VnToUtc(reportDate.AddDays(-1), 10),
                },
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 200_000,
                    PaidAt = VnToUtc(reportDate.AddDays(-2), 10),
                },
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 300_000,
                    PaidAt = VnToUtc(reportDate.AddDays(-3), 10),
                },
            };
            SetupOrderRepo(orders);

            var query = new GetDailyReportQuery { Date = reportDate, MovingAverageDays = 7 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.DailyTarget.Should().Be(200_000);
        }

        [Fact]
        public async Task Handle_Should_Exclude_ReportDate_From_MovingAverage()
        {
            // Arrange — đơn on reportDate KHÔNG được tính vào moving average
            var reportDate = new DateOnly(2026, 3, 10);

            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                // Ngày báo cáo - chỉ tính vào TotalRevenue, không phải target
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 999_999,
                    PaidAt = VnToUtc(reportDate, 9),
                },
                // Ngày trước - tính vào moving average
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 100_000,
                    PaidAt = VnToUtc(reportDate.AddDays(-1), 9),
                },
            };
            SetupOrderRepo(orders);

            var query = new GetDailyReportQuery { Date = reportDate, MovingAverageDays = 7 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert: target chỉ dựa vào ngày -1 (100k), KHÔNG bị ảnh hưởng bởi ngày báo cáo
            result.Data!.DailyTarget.Should().Be(100_000);
        }

        [Fact]
        public async Task Handle_Should_Calculate_AchievementRate()
        {
            // Arrange
            var reportDate = new DateOnly(2026, 3, 10);

            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                // Today: 800k
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 800_000,
                    PaidAt = VnToUtc(reportDate, 10),
                },
                // Yesterday: 1_000_000 → target = 1_000_000 → rate = 80%
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 1_000_000,
                    PaidAt = VnToUtc(reportDate.AddDays(-1), 10),
                },
            };
            SetupOrderRepo(orders);

            var query = new GetDailyReportQuery { Date = reportDate, MovingAverageDays = 7 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.AchievementRate.Should().BeApproximately(80.0, 0.01);
        }

        [Fact]
        public async Task Handle_Should_Respect_Timezone_Boundary()
        {
            // Arrange: đơn lúc 6h sáng VN ngày 10/3 = 23:00 UTC ngày 9/3
            // Phải được tính vào ngày 10/3 VN (không phải 9/3)
            var reportDate = new DateOnly(2026, 3, 10);
            var earlyMorningVnInUtc = VnToUtc(reportDate, 6, 0); // 06:00 VN = 23:00 UTC hôm trước

            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    TotalAmount = 500_000,
                    PaidAt = earlyMorningVnInUtc,
                },
            };
            SetupOrderRepo(orders);

            var query = new GetDailyReportQuery { Date = reportDate, MovingAverageDays = 0 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert: đơn 6h sáng VN phải nằm trong ngày 10/3
            result.Data!.TotalRevenue.Should().Be(500_000);
            result.Data.TotalOrders.Should().Be(1);
        }
    }
}

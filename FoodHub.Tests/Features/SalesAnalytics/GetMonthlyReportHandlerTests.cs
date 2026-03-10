using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetMonthlyReport;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SalesAnalytics
{
    public class GetMonthlyReportHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<GetMonthlyReportHandler>> _mockLogger;
        private readonly GetMonthlyReportHandler _handler;
        private static readonly TimeZoneInfo VnTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        public GetMonthlyReportHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GetMonthlyReportHandler>>();
            _handler = new GetMonthlyReportHandler(_mockUow.Object, _mockLogger.Object);
        }

        private void SetupOrderRepo(IEnumerable<FoodHub.Domain.Entities.Order> orders)
        {
            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
        }

        private static DateTime VnToUtc(int year, int month, int day, int hour = 0)
        {
            return TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, hour, 0, 0), VnTz);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var year = 2026;
            var month = 3;
            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    TotalAmount = 100000,
                    Status = OrderStatus.Paid,
                    PaidAt = VnToUtc(2026, 3, 10, 10),
                },
                new()
                {
                    OrderId = Guid.NewGuid(),
                    TotalAmount = 200000,
                    Status = OrderStatus.Completed,
                    PaidAt = VnToUtc(2026, 3, 15, 14),
                },
                new()
                {
                    OrderId = Guid.NewGuid(),
                    TotalAmount = 0,
                    Status = OrderStatus.Cancelled,
                    PaidAt = VnToUtc(2026, 3, 20, 16),
                },
            };
            SetupOrderRepo(orders);

            var query = new GetMonthlyReportQuery { Year = year, Month = month };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalRevenue.Should().Be(300000);
            result.Data.TotalOrders.Should().Be(2);
            result.Data.CancelledOrders.Should().Be(1);
            result.Data.Month.Should().Be(month);
            result.Data.Year.Should().Be(year);
        }

        [Fact]
        public async Task Handle_WithNoOrders_ShouldReturnZeroRevenue()
        {
            // Arrange
            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order>());
            var query = new GetMonthlyReportQuery { Year = 2026, Month = 3 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalRevenue.Should().Be(0);
            result.Data.TotalOrders.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithInvalidMonth_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetMonthlyReportQuery { Year = 2026, Month = 13 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Invalid month");
        }
    }
}

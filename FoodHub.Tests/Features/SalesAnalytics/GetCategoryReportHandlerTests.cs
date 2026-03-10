using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SalesAnalytics
{
    public class GetCategoryReportHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<GetCategoryReportHandler>> _mockLogger;
        private readonly GetCategoryReportHandler _handler;

        public GetCategoryReportHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GetCategoryReportHandler>>();
            _handler = new GetCategoryReportHandler(_mockUow.Object, _mockLogger.Object);
        }

        private void SetupOrderItemRepo(IEnumerable<OrderItem> orderItems)
        {
            var mockRepo = new Mock<IGenericRepository<OrderItem>>();
            mockRepo.Setup(r => r.Query()).Returns(orderItems.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockRepo.Object);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldReturnCategoryTotals()
        {
            // Arrange
            var cat1 = new Category { Name = "Main", CodePrefix = "M" };
            var cat2 = new Category { Name = "Drink", CodePrefix = "D" };
            var mi1 = new MenuItem
            {
                Name = "M1",
                Code = "M1",
                ImageUrl = "",
                Category = cat1,
            };
            var mi2 = new MenuItem
            {
                Name = "D1",
                Code = "D1",
                ImageUrl = "",
                Category = cat2,
            };

            var orders = new List<OrderItem>
            {
                new()
                {
                    Order = new FoodHub.Domain.Entities.Order { Status = OrderStatus.Paid },
                    MenuItem = mi1,
                    ItemNameSnapshot = "M1",
                    ItemCodeSnapshot = "M1",
                    StationSnapshot = "K",
                    Quantity = 2,
                    UnitPriceSnapshot = 50000,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
                new()
                {
                    Order = new FoodHub.Domain.Entities.Order { Status = OrderStatus.Completed },
                    MenuItem = mi1,
                    ItemNameSnapshot = "M1",
                    ItemCodeSnapshot = "M1",
                    StationSnapshot = "K",
                    Quantity = 1,
                    UnitPriceSnapshot = 50000,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
                new()
                {
                    Order = new FoodHub.Domain.Entities.Order { Status = OrderStatus.Paid },
                    MenuItem = mi2,
                    ItemNameSnapshot = "D1",
                    ItemCodeSnapshot = "D1",
                    StationSnapshot = "K",
                    Quantity = 5,
                    UnitPriceSnapshot = 20000,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
            };
            SetupOrderItemRepo(orders);

            var query = new GetCategoryReportQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.First(c => c.CategoryName == "Main").TotalRevenue.Should().Be(150000); // 3 * 50000
            result
                .Data.Items.First(c => c.CategoryName == "Drink")
                .TotalRevenue.Should()
                .Be(100000); // 5 * 20000
        }

        [Fact]
        public async Task Handle_WithNoItems_ShouldReturnEmptyList()
        {
            // Arrange
            SetupOrderItemRepo(new List<OrderItem>());
            var query = new GetCategoryReportQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldRespectDateRange()
        {
            // Arrange
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var cat = new Category { Name = "Main", CodePrefix = "M" };
            var mi = new MenuItem
            {
                Name = "M1",
                Code = "M1",
                ImageUrl = "",
                Category = cat,
            };
            var date = new DateOnly(2026, 3, 10);
            var dateUtc = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), tz);

            var orderItems = new List<OrderItem>
            {
                new()
                {
                    Order = new FoodHub.Domain.Entities.Order
                    {
                        Status = OrderStatus.Paid,
                        PaidAt = dateUtc,
                    },
                    MenuItem = mi,
                    ItemNameSnapshot = "M1",
                    ItemCodeSnapshot = "M1",
                    StationSnapshot = "K",
                    Quantity = 1,
                    UnitPriceSnapshot = 50000,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
                new()
                {
                    Order = new FoodHub.Domain.Entities.Order
                    {
                        Status = OrderStatus.Paid,
                        PaidAt = dateUtc.AddDays(-1),
                    },
                    MenuItem = mi,
                    ItemNameSnapshot = "M1",
                    ItemCodeSnapshot = "M1",
                    StationSnapshot = "K",
                    Quantity = 1,
                    UnitPriceSnapshot = 50000,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
            };
            SetupOrderItemRepo(orderItems);

            var query = new GetCategoryReportQuery { StartDate = date, EndDate = date };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().ItemCount.Should().Be(1);
        }
    }
}

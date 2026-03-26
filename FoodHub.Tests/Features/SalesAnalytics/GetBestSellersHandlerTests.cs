using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;
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
    public class GetBestSellersHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<GetBestSellersHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCache;
        private readonly GetBestSellersHandler _handler;

        public GetBestSellersHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GetBestSellersHandler>>();
            _mockCache = new Mock<ICacheService>();
            _mockCache
                .Setup(c =>
                    c.GetAsync<GetBestSellersResponse>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetBestSellersResponse?)null);

            _handler = new GetBestSellersHandler(
                _mockUow.Object,
                _mockLogger.Object,
                _mockCache.Object
            );
        }

        private void SetupRepos(
            IEnumerable<FoodHub.Domain.Entities.Order> orders,
            IEnumerable<OrderItem> orderItems
        )
        {
            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockOrderRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo.Setup(r => r.Query()).Returns(orderItems.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldReturnTopSellers()
        {
            // Arrange
            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    PaidAt = DateTime.UtcNow,
                },
            };

            var category = new Category { Name = "Food", CodePrefix = "F" };
            var menuItem1 = new MenuItem
            {
                MenuItemId = Guid.NewGuid(),
                Name = "Pizza",
                Code = "P1",
                ImageUrl = "",
                CostPrice = 50000,
                Category = category,
            };
            var menuItem2 = new MenuItem
            {
                MenuItemId = Guid.NewGuid(),
                Name = "Burger",
                Code = "B1",
                ImageUrl = "",
                CostPrice = 30000,
                Category = category,
            };

            var orderItems = new List<OrderItem>
            {
                new()
                {
                    OrderId = orders[0].OrderId,
                    MenuItemId = menuItem1.MenuItemId,
                    MenuItem = menuItem1,
                    Quantity = 5,
                    UnitPriceSnapshot = 100000,
                    ItemNameSnapshot = "Pizza",
                    ItemCodeSnapshot = "P1",
                    StationSnapshot = "Kitchen",
                    Status = OrderItemStatus.Completed,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
                new()
                {
                    OrderId = orders[0].OrderId,
                    MenuItemId = menuItem2.MenuItemId,
                    MenuItem = menuItem2,
                    Quantity = 10,
                    UnitPriceSnapshot = 50000,
                    ItemNameSnapshot = "Burger",
                    ItemCodeSnapshot = "B1",
                    StationSnapshot = "Kitchen",
                    Status = OrderItemStatus.Completed,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
            };

            SetupRepos(orders, orderItems);

            var query = new GetBestSellersQuery { Top = 5 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result
                .Data.Items.OrderByDescending(x => x.QuantitySold)
                .First()
                .ItemName.Should()
                .Be("Burger");
        }

        [Fact]
        public async Task Handle_WithNoOrders_ShouldReturnEmptyList()
        {
            // Arrange
            SetupRepos(new List<FoodHub.Domain.Entities.Order>(), new List<OrderItem>());
            var query = new GetBestSellersQuery { Top = 5 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithOnlyCancelledItems_ShouldExcludeThem()
        {
            // Arrange
            var orders = new List<FoodHub.Domain.Entities.Order>
            {
                new()
                {
                    OrderId = Guid.NewGuid(),
                    Status = OrderStatus.Paid,
                    PaidAt = DateTime.UtcNow,
                },
            };
            var category = new Category { Name = "Food", CodePrefix = "F" };
            var menuItem = new MenuItem
            {
                MenuItemId = Guid.NewGuid(),
                Name = "Test",
                Code = "T1",
                ImageUrl = "",
                CostPrice = 10000,
                Category = category,
            };

            var orderItems = new List<OrderItem>
            {
                new()
                {
                    OrderId = orders[0].OrderId,
                    MenuItemId = menuItem.MenuItemId,
                    MenuItem = menuItem,
                    Quantity = 5,
                    ItemNameSnapshot = "Test",
                    ItemCodeSnapshot = "T1",
                    StationSnapshot = "Kitchen",
                    Status = OrderItemStatus.Cancelled,
                    OptionGroups = new List<OrderItemOptionGroup>(),
                },
            };
            SetupRepos(orders, orderItems);

            var query = new GetBestSellersQuery { Top = 5 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
        }
    }
}

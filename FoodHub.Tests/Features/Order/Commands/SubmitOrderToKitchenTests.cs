using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Order.Commands
{
    public class SubmitOrderToKitchenTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly SubmitOrderToKitchenHandler _handler;

        public SubmitOrderToKitchenTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();

            _handler = new SubmitOrderToKitchenHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderSubmitted_ForTakeaway()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = "Test note",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuItemId = menuItemId,
                        Quantity = 2,
                        Note = "Extra spicy",
                        SelectedOptions = null,
                    },
                },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI001",
                Name = "Test Item",
                PriceTakeAway = 50,
                Station = Station.HotKitchen,
                ImageUrl = "",
            };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            var mockMenuRepo = new Mock<IGenericRepository<MenuItem>>();
            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());
            mockMenuRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()),
                Times.Once
            );
            _mockUow.Verify(
                u => u.Repository<OrderAuditLog>().AddAsync(It.IsAny<OrderAuditLog>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderSubmitted_ForDineIn_WithValidTable()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tableId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.DineIn,
                TableId = tableId,
                Note = null,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuItemId = menuItemId,
                        Quantity = 1,
                        Note = null,
                        SelectedOptions = null,
                    },
                },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI002",
                Name = "Dine-in Item",
                PriceDineIn = 75,
                Station = Station.Bar,
                ImageUrl = "",
            };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            var mockMenuRepo = new Mock<IGenericRepository<MenuItem>>();
            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());
            mockMenuRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(
                            It.Is<FoodHub.Domain.Entities.Order>(o =>
                                o.OrderType == OrderType.DineIn
                                && o.TableId == tableId
                                && o.Status == OrderStatus.Serving
                            )
                        ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DineIn_WithoutTableId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.DineIn,
                TableId = null,
                Note = "Test",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuItemId = Guid.NewGuid(),
                        Quantity = 1,
                        Note = null,
                        SelectedOptions = null,
                    },
                },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.SelectTable))
                .Returns("Please select a table for dine-in orders.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TableAlreadyOccupied()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tableId = Guid.NewGuid();
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.DineIn,
                TableId = tableId,
                Note = null,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuItemId = Guid.NewGuid(),
                        Quantity = 1,
                        Note = null,
                        SelectedOptions = null,
                    },
                },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var existingOrder = new FoodHub.Domain.Entities.Order
            {
                TableId = tableId,
                Status = OrderStatus.Serving,
            };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Order> { existingOrder }
                        .AsQueryable()
                        .BuildMock()
                );

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.TableAlreadyOccupied))
                .Returns("Table is already occupied.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = null,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuItemId = Guid.NewGuid(), // Non-existent
                        Quantity = 1,
                        Note = null,
                        SelectedOptions = null,
                    },
                },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockMenuRepo = new Mock<IGenericRepository<MenuItem>>();
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuRepo.Object);

            mockMenuRepo
                .Setup(r => r.Query())
                .Returns(new List<MenuItem>().AsQueryable().BuildMock());

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.MenuItem.NotFound))
                .Returns("Menu item not found.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ItemOutOfStock()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = null,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuItemId = menuItemId,
                        Quantity = 1,
                        Note = null,
                        SelectedOptions = null,
                    },
                },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI004",
                Name = "Out of Stock Item",
                IsOutOfStock = true,
                ImageUrl = "",
            };

            var mockMenuRepo = new Mock<IGenericRepository<MenuItem>>();
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuRepo.Object);

            mockMenuRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.MenuItem.OutOfStock))
                .Returns("Item out of stock: ");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = null,
                Items = new List<OrderItemDto>(),
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns((string)null);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.UserNotLoggedIn))
                .Returns("User not logged in.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
            _mockUow.Verify(
                u => u.Repository<FoodHub.Domain.Entities.Order>().Query(),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_MergeDuplicateItems()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new SubmitOrderToKitchenCommand
            {
                OrderType = OrderType.Takeaway,
                TableId = null,
                Note = null,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuItemId = menuItemId,
                        Quantity = 2,
                        Note = "Same note",
                        SelectedOptions = null,
                    },
                    new OrderItemDto
                    {
                        MenuItemId = menuItemId,
                        Quantity = 3,
                        Note = "Same note",
                        SelectedOptions = null,
                    },
                },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Code = "MI003",
                Name = "Merge Test Item",
                PriceTakeAway = 25,
                Station = Station.HotKitchen,
                ImageUrl = "",
            };

            var mockOrderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            var mockMenuRepo = new Mock<IGenericRepository<MenuItem>>();
            var mockAuditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<FoodHub.Domain.Entities.Order>().AsQueryable().BuildMock());
            mockMenuRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(
                            It.Is<FoodHub.Domain.Entities.Order>(o =>
                                o.OrderItems.Count == 1 && o.OrderItems.First().Quantity == 5 // 2 + 3 merged
                            )
                        ),
                Times.Once
            );
        }
    }
}

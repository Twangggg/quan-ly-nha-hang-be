using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.OrderItems.Commands.AddOrderItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.OrderItems.Commands
{
    public class AddOrderItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly AddOrderItemHandler _handler;

        public AddOrderItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _handler = new AddOrderItemHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var command = new AddOrderItemCommand
            {
                OrderId = Guid.NewGuid(),
                MenuItemId = Guid.NewGuid(),
                Quantity = 1,
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(string.Empty);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.UserNotLoggedIn))
                .Returns("User not logged in");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new AddOrderItemCommand
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = 1,
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<Domain.Entities.Order>().AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.NotFound))
                .Returns("Order not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderIsCompleted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new AddOrderItemCommand
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = 1,
            };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Completed,
                OrderType = OrderType.DineIn,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>(),
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<Domain.Entities.Order> { existingOrder }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.InvalidAction))
                .Returns("Invalid action");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new AddOrderItemCommand
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = 1,
            };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>(),
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<Domain.Entities.Order> { existingOrder }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo
                .Setup(r => r.Query())
                .Returns(new List<MenuItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.MenuItem.NotFound))
                .Returns("Menu item not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MenuItemIsOutOfStock()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new AddOrderItemCommand
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = 1,
            };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>(),
            };

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Name = "Pizza",
                Code = "P001",
                PriceDineIn = 10.00m,
                PriceTakeAway = 9.00m,
                IsOutOfStock = true,
                Station = Station.HotKitchen,
                ImageUrl = "test.jpg",
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<Domain.Entities.Order> { existingOrder }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.MenuItem.OutOfStock))
                .Returns("Menu item out of stock");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderItemAdded()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new AddOrderItemCommand
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = 2,
                Note = "No cheese",
            };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                Status = OrderStatus.Serving,
                OrderType = OrderType.DineIn,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>(),
            };

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                Name = "Pizza",
                Code = "P001",
                PriceDineIn = 10.00m,
                PriceTakeAway = 9.00m,
                IsOutOfStock = false,
                Station = Station.HotKitchen,
                ImageUrl = "test.jpg",
                OptionGroups = new List<OptionGroup>(),
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<Domain.Entities.Order> { existingOrder }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            var mockMenuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            mockMenuItemRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<MenuItem> { menuItem }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            var mockOrderAuditLogRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            mockOrderAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<OrderAuditLog>()));
            _mockUow
                .Setup(u => u.Repository<OrderAuditLog>())
                .Returns(mockOrderAuditLogRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

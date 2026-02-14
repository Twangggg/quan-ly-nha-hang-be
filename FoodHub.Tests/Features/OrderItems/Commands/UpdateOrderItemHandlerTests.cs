using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.OrderItems.Commands
{
    public class UpdateOrderItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UpdateOrderItemCommand>> _mockLogger;
        private readonly UpdateOrderItemHandler _handler;

        public UpdateOrderItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UpdateOrderItemCommand>>();
            _handler = new UpdateOrderItemHandler(
                _mockUow.Object,
                _mockMessageService.Object,
                _mockCurrentUserService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var command = new UpdateOrderItemCommand
            {
                OrderId = orderId,
                Items = new List<UpdateOrderItemDto>(),
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(string.Empty);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.UserNotLoggedIn))
                .Returns("User not logged in");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var command = new UpdateOrderItemCommand
            {
                OrderId = orderId,
                Items = new List<UpdateOrderItemDto>(),
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
        public async Task Handle_Should_ReturnFailure_When_MenuItemNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new UpdateOrderItemCommand
            {
                OrderId = orderId,
                Items = new List<UpdateOrderItemDto>
                {
                    new UpdateOrderItemDto(
                        OrderItemId: null,
                        MenuItemId: menuItemId,
                        Quantity: 1,
                        ItemNote: null,
                        SelectedOptions: null
                    ),
                },
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
            mockMenuItemRepo.Setup(r => r.GetByIdAsync(menuItemId)).ReturnsAsync((MenuItem?)null);
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
        public async Task Handle_Should_ReturnSuccess_When_OrderUpdated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();
            var command = new UpdateOrderItemCommand
            {
                OrderId = orderId,
                Items = new List<UpdateOrderItemDto>
                {
                    new UpdateOrderItemDto(
                        OrderItemId: null,
                        MenuItemId: menuItemId,
                        Quantity: 2,
                        ItemNote: "Test note",
                        SelectedOptions: null
                    ),
                },
                Reason = "Updated order",
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
            mockMenuItemRepo.Setup(r => r.GetByIdAsync(menuItemId)).ReturnsAsync(menuItem);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(mockMenuItemRepo.Object);

            var mockAuditLogRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<OrderAuditLog>()));
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditLogRepo.Object);

            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockMapper
                .Setup(m => m.Map<UpdateOrderItemResponse>(It.IsAny<Domain.Entities.Order>()))
                .Returns(new UpdateOrderItemResponse());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

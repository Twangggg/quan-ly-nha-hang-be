using FluentAssertions;
using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem;
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

namespace FoodHub.Tests.Features.OrderItems.Commands
{
    public class CancelOrderItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ISignalRService> _mockSignalRService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CancelOrderItemHandler>> _mockLogger;
        private readonly CancelOrderItemHandler _handler;

        public CancelOrderItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockSignalRService = new Mock<ISignalRService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CancelOrderItemHandler>>();
            _mockMapper
                .Setup(x => x.Map<CancelOrderItemResponse>(It.IsAny<object>()))
                .Returns(new CancelOrderItemResponse());
            _handler = new CancelOrderItemHandler(
                _mockUow.Object,
                _mockMessageService.Object,
                _mockCurrentUserService.Object,
                _mockSignalRService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                Guid.NewGuid(),
                orderItemId,
                "Customer requested",
                new Domain.Entities.Order()
            );

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
        public async Task Handle_Should_ReturnFailure_When_OrderItemNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                orderId,
                orderItemId,
                "Customer requested",
                new Domain.Entities.Order()
            );

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(new List<OrderItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.OrderItem.NotFound))
                .Returns("Order item not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OrderItemStatusIsNotPreparing()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                orderId,
                orderItemId,
                "Customer requested",
                new Domain.Entities.Order()
            );

            var existingOrderItem = new OrderItem
            {
                OrderItemId = orderItemId,
                OrderId = orderId,
                Status = OrderItemStatus.Completed, // Not Preparing
                Quantity = 1,
                UnitPriceSnapshot = 10.00m,
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<OrderItem> { existingOrderItem }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Order.InvalidActionWithStatus))
                .Returns("Invalid action with current status");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OrderItemCancelled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var command = new CancelOrderItemCommand(
                orderId,
                orderItemId,
                "Customer requested",
                new Domain.Entities.Order()
            );

            var existingOrderItem = new OrderItem
            {
                OrderItemId = orderItemId,
                OrderId = orderId,
                Status = OrderItemStatus.Preparing,
                Quantity = 2,
                UnitPriceSnapshot = 10.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                TotalAmount = 20.00m,
                OrderItems = new List<OrderItem> { existingOrderItem },
            };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<OrderItem> { existingOrderItem }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

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

            var mockAuditLogRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<OrderAuditLog>()));
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditLogRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingOrderItem.Status.Should().Be(OrderItemStatus.Cancelled);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_CancelComboParentAndAllChildren_When_CancellingComboParent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var comboParentId = Guid.NewGuid();
            var child1Id = Guid.NewGuid();
            var child2Id = Guid.NewGuid();
            var child3Id = Guid.NewGuid();

            var command = new CancelOrderItemCommand(
                orderId,
                comboParentId,
                "Customer requested",
                new Domain.Entities.Order()
            );

            // Combo parent with Preparing status
            var comboParent = new OrderItem
            {
                OrderItemId = comboParentId,
                OrderId = orderId,
                Status = OrderItemStatus.Preparing,
                Quantity = 1,
                UnitPriceSnapshot = 50.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            // Child 1: Already Cooking (should be cancelled)
            var child1 = new OrderItem
            {
                OrderItemId = child1Id,
                OrderId = orderId,
                ComboParentOrderItemId = comboParentId,
                Status = OrderItemStatus.Cooking,
                Quantity = 1,
                UnitPriceSnapshot = 20.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            // Child 2: Preparing (should be cancelled)
            var child2 = new OrderItem
            {
                OrderItemId = child2Id,
                OrderId = orderId,
                ComboParentOrderItemId = comboParentId,
                Status = OrderItemStatus.Preparing,
                Quantity = 1,
                UnitPriceSnapshot = 15.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            // Child 3: Already Completed (should still be cancelled as part of combo)
            var child3 = new OrderItem
            {
                OrderItemId = child3Id,
                OrderId = orderId,
                ComboParentOrderItemId = comboParentId,
                Status = OrderItemStatus.Completed,
                Quantity = 1,
                UnitPriceSnapshot = 15.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            // A regular item (not part of combo) - should NOT be affected
            var regularItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = orderId,
                ComboParentOrderItemId = null,
                Status = OrderItemStatus.Preparing,
                Quantity = 1,
                UnitPriceSnapshot = 10.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            var allItems = new List<OrderItem> { comboParent, child1, child2, child3, regularItem };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(allItems.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                TotalAmount = 100.00m,
                OrderItems = allItems,
                Promotion = null,
            };

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<Domain.Entities.Order> { existingOrder }.AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            var mockAuditLogRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<OrderAuditLog>()));
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditLogRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Parent should be cancelled
            comboParent.Status.Should().Be(OrderItemStatus.Cancelled);

            // All combo children should be cancelled
            child1.Status.Should().Be(OrderItemStatus.Cancelled, "Child1 (Cooking) should be cancelled when parent combo is cancelled");
            child2.Status.Should().Be(OrderItemStatus.Cancelled, "Child2 (Preparing) should be cancelled when parent combo is cancelled");
            child3.Status.Should().Be(OrderItemStatus.Cancelled, "Child3 (Completed) should be cancelled when parent combo is cancelled");

            // Regular item should NOT be affected
            regularItem.Status.Should().Be(OrderItemStatus.Preparing, "Regular item not part of combo should not be affected");

            // Combo parent references should be cleared
            child1.ComboParentOrderItemId.Should().BeNull();
            child2.ComboParentOrderItemId.Should().BeNull();
            child3.ComboParentOrderItemId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_CancelChild_When_CancellingComboChild()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var comboParentId = Guid.NewGuid();
            var childId = Guid.NewGuid();

            var command = new CancelOrderItemCommand(
                orderId,
                childId,
                "Customer requested",
                new Domain.Entities.Order()
            );

            var comboParent = new OrderItem
            {
                OrderItemId = comboParentId,
                OrderId = orderId,
                Status = OrderItemStatus.Preparing,
                Quantity = 1,
                UnitPriceSnapshot = 50.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            var comboChild = new OrderItem
            {
                OrderItemId = childId,
                OrderId = orderId,
                ComboParentOrderItemId = comboParentId,
                Status = OrderItemStatus.Preparing,
                Quantity = 1,
                UnitPriceSnapshot = 20.00m,
                OptionGroups = new List<OrderItemOptionGroup>(),
            };

            var allItems = new List<OrderItem> { comboParent, comboChild };

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var mockOrderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            mockOrderItemRepo
                .Setup(r => r.Query())
                .Returns(allItems.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(mockOrderItemRepo.Object);

            var existingOrder = new Domain.Entities.Order
            {
                OrderId = orderId,
                TotalAmount = 70.00m,
                OrderItems = allItems,
                Promotion = null,
            };

            var mockOrderRepo = new Mock<IGenericRepository<Domain.Entities.Order>>();
            mockOrderRepo
                .Setup(r => r.Query())
                .Returns(new List<Domain.Entities.Order> { existingOrder }.AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<Domain.Entities.Order>())
                .Returns(mockOrderRepo.Object);

            var mockAuditLogRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            mockAuditLogRepo.Setup(r => r.AddAsync(It.IsAny<OrderAuditLog>()));
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(mockAuditLogRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Child should be cancelled
            comboChild.Status.Should().Be(OrderItemStatus.Cancelled);

            // Child's ComboParentOrderItemId should be cleared
            comboChild.ComboParentOrderItemId.Should().BeNull();

            // Parent should NOT be cancelled (only the child was cancelled)
            comboParent.Status.Should().Be(OrderItemStatus.Preparing);
        }
    }
}

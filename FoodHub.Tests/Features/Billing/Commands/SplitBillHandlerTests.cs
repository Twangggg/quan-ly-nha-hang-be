using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Billing.Commands.SplitBill;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Billing.Commands
{
    public class SplitBillHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
        private readonly Mock<IMessageService> _mockMessageService = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ILogger<SplitBillHandler>> _mockLogger = new();

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_SplitBillValid()
        {
            var userId = Guid.NewGuid();
            var sourceOrderId = Guid.NewGuid();
            var sourceItemId = Guid.NewGuid();
            var sourceOrder = new FoodHub.Domain.Entities.Order
            {
                OrderId = sourceOrderId,
                OrderCode = "ORD-20260323-0001",
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = Guid.NewGuid(),
                TotalAmount = 100000,
                IsPriority = false,
                OrderItems = new List<OrderItem>
                {
                    new()
                    {
                        OrderItemId = sourceItemId,
                        OrderId = sourceOrderId,
                        MenuItemId = Guid.NewGuid(),
                        ItemNameSnapshot = "Phở bò",
                        ItemCodeSnapshot = "PHO01",
                        StationSnapshot = "Kitchen",
                        Status = OrderItemStatus.Preparing,
                        Quantity = 2,
                        UnitPriceSnapshot = 50000,
                        OptionGroups = new List<OrderItemOptionGroup>(),
                    },
                },
            };

            var orderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            var orderItemRepo = new Mock<IGenericRepository<OrderItem>>();
            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();

            orderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Order> { sourceOrder }
                        .AsQueryable()
                        .BuildMock()
                );
            orderRepo
                .Setup(r => r.AddAsync(It.IsAny<FoodHub.Domain.Entities.Order>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);
            orderRepo.Setup(r => r.Update(It.IsAny<FoodHub.Domain.Entities.Order>()));
            orderItemRepo.Setup(r => r.Delete(It.IsAny<OrderItem>()));
            auditRepo.Setup(r => r.AddAsync(It.IsAny<OrderAuditLog>())).Returns(Task.CompletedTask);

            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(orderRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderItem>()).Returns(orderItemRepo.Object);
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);
            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            _mockMapper
                .Setup(m => m.Map<List<SplitBillItemDto>>(It.IsAny<List<OrderItem>>()))
                .Returns<List<OrderItem>>(items =>
                    items
                        .Select(item => new SplitBillItemDto
                        {
                            OrderItemId = item.OrderItemId,
                            OrderId = item.OrderId,
                            MenuItemId = item.MenuItemId,
                            ItemNameSnapshot = item.ItemNameSnapshot,
                            Quantity = item.Quantity,
                            UnitPriceSnapshot = item.UnitPriceSnapshot,
                        })
                        .ToList()
                );

            var handler = new SplitBillHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new SplitBillCommand
                {
                    OrderId = sourceOrderId,
                    ItemsToSplit = new List<SplitBillItemCommand>
                    {
                        new() { OrderItemId = sourceItemId, QuantityToSplit = 1 },
                    },
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SourceOrderId.Should().Be(sourceOrderId);
            result.Data.DestinationOrderId.Should().NotBeEmpty();
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            orderItemRepo.Verify(r => r.Delete(It.IsAny<OrderItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_QuantityExceedsSourceItem()
        {
            var userId = Guid.NewGuid();
            var sourceOrderId = Guid.NewGuid();
            var sourceItemId = Guid.NewGuid();
            var sourceOrder = new FoodHub.Domain.Entities.Order
            {
                OrderId = sourceOrderId,
                OrderCode = "ORD-20260323-0002",
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Serving,
                TableId = Guid.NewGuid(),
                TotalAmount = 100000,
                OrderItems = new List<OrderItem>
                {
                    new()
                    {
                        OrderItemId = sourceItemId,
                        OrderId = sourceOrderId,
                        MenuItemId = Guid.NewGuid(),
                        ItemNameSnapshot = "Cơm gà",
                        ItemCodeSnapshot = "COM01",
                        StationSnapshot = "Kitchen",
                        Status = OrderItemStatus.Preparing,
                        Quantity = 1,
                        UnitPriceSnapshot = 40000,
                        OptionGroups = new List<OrderItemOptionGroup>(),
                    },
                },
            };

            var orderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            orderRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Order> { sourceOrder }
                        .AsQueryable()
                        .BuildMock()
                );

            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(orderRepo.Object);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var handler = new SplitBillHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );

            var result = await handler.Handle(
                new SplitBillCommand
                {
                    OrderId = sourceOrderId,
                    ItemsToSplit = new List<SplitBillItemCommand>
                    {
                        new() { OrderItemId = sourceItemId, QuantityToSplit = 2 },
                    },
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }
    }
}

using FluentAssertions;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Xunit;

namespace FoodHub.Tests.Features.Order.Domain
{
    public class OrderTests
    {
        [Fact]
        public void Complete_Should_Fail_When_OrderHasUnfinishedItems()
        {
            var order = new FoodHub.Domain.Entities.Order
            {
                Status = OrderStatus.Serving,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { Status = OrderItemStatus.Preparing },
                },
            };

            var result = order.Complete();

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(DomainErrors.Order.ItemsNotFinished);
            order.Status.Should().Be(OrderStatus.Serving);
        }

        [Fact]
        public void Cancel_Should_Fail_When_OrderContainsCompletedItem()
        {
            var order = new FoodHub.Domain.Entities.Order
            {
                Status = OrderStatus.Serving,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { Status = OrderItemStatus.Completed },
                },
            };

            var result = order.Cancel();

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(DomainErrors.Order.InvalidStatusForCancel);
            order.Status.Should().Be(OrderStatus.Serving);
        }

        [Fact]
        public void Complete_Should_Succeed_When_ComboParent_Still_Preparing_But_All_Children_Finished()
        {
            var comboParentId = Guid.NewGuid();
            var order = new FoodHub.Domain.Entities.Order
            {
                Status = OrderStatus.Serving,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        OrderItemId = comboParentId,
                        Status = OrderItemStatus.Preparing,
                        Quantity = 1,
                        UnitPriceSnapshot = 100m,
                    },
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        ComboParentOrderItemId = comboParentId,
                        Status = OrderItemStatus.Completed,
                        Quantity = 1,
                        UnitPriceSnapshot = 0m,
                    },
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        ComboParentOrderItemId = comboParentId,
                        Status = OrderItemStatus.Cancelled,
                        Quantity = 1,
                        UnitPriceSnapshot = 0m,
                    },
                },
            };

            var result = order.Complete();

            result.IsSuccess.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Completed);
            order.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public void GetCountableKitchenItems_Should_Exclude_ComboParent_Placeholder()
        {
            var comboParentId = Guid.NewGuid();
            var order = new FoodHub.Domain.Entities.Order
            {
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        OrderItemId = comboParentId,
                        MenuItemId = null,
                        Status = OrderItemStatus.Preparing,
                    },
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        ComboParentOrderItemId = comboParentId,
                        Status = OrderItemStatus.Cooking,
                    },
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        ComboParentOrderItemId = comboParentId,
                        Status = OrderItemStatus.Cooking,
                    },
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        ComboParentOrderItemId = comboParentId,
                        Status = OrderItemStatus.Cooking,
                    },
                },
            };

            var countableItems = order.GetCountableKitchenItems();

            countableItems.Should().HaveCount(3);
            countableItems.Should().OnlyContain(item => item.ComboParentOrderItemId == comboParentId);
            order.GetPendingKitchenItems().Should().HaveCount(3);
        }
    }
}

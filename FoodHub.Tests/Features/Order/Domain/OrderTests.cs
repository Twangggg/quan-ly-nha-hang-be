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
    }
}

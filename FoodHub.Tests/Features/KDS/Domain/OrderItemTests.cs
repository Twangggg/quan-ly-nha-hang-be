using FluentAssertions;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Xunit;

namespace FoodHub.Tests.Features.KDS.Domain
{
    public class OrderItemTests
    {
        [Fact]
        public void StartCooking_Should_Succeed_When_StatusIsPreparing()
        {
            var orderItem = new OrderItem { Status = OrderItemStatus.Preparing };

            var result = orderItem.StartCooking();

            result.IsSuccess.Should().BeTrue();
            orderItem.Status.Should().Be(OrderItemStatus.Cooking);
        }

        [Fact]
        public void StartCooking_Should_Fail_When_StatusIsNotPreparing()
        {
            var orderItem = new OrderItem { Status = OrderItemStatus.Completed };

            var result = orderItem.StartCooking();

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(DomainErrors.OrderItem.MustBePreparingToStartCooking);
            orderItem.Status.Should().Be(OrderItemStatus.Completed);
        }

        [Fact]
        public void CompleteCooking_Should_Succeed_When_StatusIsCooking()
        {
            var orderItem = new OrderItem { Status = OrderItemStatus.Cooking };

            var result = orderItem.CompleteCooking();

            result.IsSuccess.Should().BeTrue();
            orderItem.Status.Should().Be(OrderItemStatus.Completed);
        }

        [Fact]
        public void Reject_Should_Succeed_With_ValidReason()
        {
            var orderItem = new OrderItem { Status = OrderItemStatus.Cooking };
            var reason = "Het nguyen lieu";

            var result = orderItem.Reject(reason);

            result.IsSuccess.Should().BeTrue();
            orderItem.Status.Should().Be(OrderItemStatus.Rejected);
            orderItem.RejectionReason.Should().Be(reason);
            orderItem.RejectedAt.Should().NotBeNull();
        }
    }
}

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
            // Arrange
            var orderItem = new OrderItem { Status = OrderItemStatus.Preparing };

            // Act
            var result = orderItem.StartCooking();

            // Assert
            result.IsSuccess.Should().BeTrue();
            orderItem.Status.Should().Be(OrderItemStatus.Cooking);
        }

        [Fact]
        public void StartCooking_Should_Fail_When_StatusIsNotPreparing()
        {
            // Arrange
            var orderItem = new OrderItem
            {
                Status = OrderItemStatus.Ready, // Trạng thái sai
            };

            // Act
            var result = orderItem.StartCooking();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(DomainErrors.OrderItem.MustBePreparingToStartCooking);
            orderItem.Status.Should().Be(OrderItemStatus.Ready); // Không đổi
        }

        [Fact]
        public void MarkReady_Should_Succeed_When_StatusIsCooking()
        {
            // Arrange
            var orderItem = new OrderItem { Status = OrderItemStatus.Cooking };

            // Act
            var result = orderItem.MarkReady();

            // Assert
            result.IsSuccess.Should().BeTrue();
            orderItem.Status.Should().Be(OrderItemStatus.Ready);
        }

        [Fact]
        public void Reject_Should_Succeed_With_ValidReason()
        {
            // Arrange
            var orderItem = new OrderItem { Status = OrderItemStatus.Cooking };
            var reason = "Hết nguyên liệu";

            // Act
            var result = orderItem.Reject(reason);

            // Assert
            result.IsSuccess.Should().BeTrue();
            orderItem.Status.Should().Be(OrderItemStatus.Rejected);
            orderItem.RejectionReason.Should().Be(reason);
            orderItem.RejectedAt.Should().NotBeNull();
        }
    }
}

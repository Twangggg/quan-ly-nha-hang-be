using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen
{
    public class SubmitOrderToKitchenCommand : IRequest<Result<Guid>>
    {
        public Guid? TableId { get; set; }
        //Order type - defaults to DINE-IN when selecting a table
        public OrderType OrderType { get; init; } = OrderType.DineIn;
        public string? Note { get; set; }
        public List<OrderItemDto> Items { get; init; } = new();
    }

    public record OrderItemDto
    {
        public Guid MenuItemId { get; init; }
        public int Quantity { get; init; }
        public string? Note { get; init; }
    }
}

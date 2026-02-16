using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.OrderItems.Commands.AddOrderItem;
using MediatR;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemCommand : IRequest<Result<UpdateOrderItemResponse>>
    {
        public Guid OrderId { get; set; }
        public string? Reason { get; set; }
        public List<UpdateOrderItemDto> Items { get; set; } = new List<UpdateOrderItemDto>();

    }

    public record UpdateOrderItemDto(
        Guid? OrderItemId,
        Guid MenuItemId,
        int Quantity,
        string? ItemNote,
        List<OrderItemOptionGroupDto>? SelectedOptions = null
        );
}

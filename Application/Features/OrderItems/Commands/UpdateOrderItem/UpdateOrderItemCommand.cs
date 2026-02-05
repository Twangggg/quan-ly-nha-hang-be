using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemCommand : IRequest<Result<UpdateOrderItemResponse>>
    {
        public Guid OrderId { get; set; }
        public Guid OrderItemId { get; set; }
        public string? Reason { get; set; }
        public List<UpdateOrderItemDto> Items { get; set; } = new List<UpdateOrderItemDto>();

    }

    public record UpdateOrderItemDto(
        Guid? OrderItemId,
        Guid MenuItemId,
        int Quantity,
        string? ItemNote
        );
}

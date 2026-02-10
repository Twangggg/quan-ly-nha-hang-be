using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemCommand : IRequest<Result<Guid>>
    {
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public string? Reason { get; set; }
        public List<OrderItemOptionGroupDto>? SelectedOptions { get; set; }
    }

    public class OrderItemOptionGroupDto
    {
        public Guid OptionGroupId { get; set; }
        public List<OrderItemOptionValueDto> SelectedValues { get; set; } = new();
    }

    public class OrderItemOptionValueDto
    {
        public Guid OptionItemId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }
}

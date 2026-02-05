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
    }
}

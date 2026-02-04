using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Order.Commands.UpdateDraftOrder
{
    public class UpdateDraftOrderCommand : IRequest<Result<Guid>>
    {
        public Guid OrderItemId { get; set; }
        public string? Note { get; set; } = null;
        public OrderType OrderType { get; set; }
        public ICollection<Domain.Entities.OrderItem> OrderItems { get; set; } = new List<Domain.Entities.OrderItem>();
    }
}

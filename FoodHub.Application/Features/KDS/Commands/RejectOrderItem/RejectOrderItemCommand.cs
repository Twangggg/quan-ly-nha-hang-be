using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.RejectOrderItem
{
    public class RejectOrderItemCommand : IRequest<Result<Guid>>
    {
        public Guid OrderItemId { get; set; }
        public string Reason { get; set; } = null!;
    }
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.ReturnOrderItem
{
    public class ReturnOrderItemCommand : IRequest<Result<Guid>>
    {
        public Guid OrderItemId { get; set; }
    }
}

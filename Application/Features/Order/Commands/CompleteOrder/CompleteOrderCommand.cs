using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Order.Commands.CompleteOrder
{
    public class CompleteOrderCommand : IRequest<Result<CompleteOrderResponse>>
    {
        public Guid OrderId
        {
            get; set;
        }
    }
}
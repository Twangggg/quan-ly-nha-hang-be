using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Order.Commands.SubmitOrder
{
    public class SubmitOrderCommand : IRequest<Result<Guid>>
    {
        public Guid OrderId { get; set; }    
    }
}

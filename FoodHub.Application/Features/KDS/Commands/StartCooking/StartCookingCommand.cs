using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.StartCooking
{
    public class StartCookingCommand : IRequest<Result<Guid>>
    {
        public Guid OrderItemId { get; set; }
    }
}

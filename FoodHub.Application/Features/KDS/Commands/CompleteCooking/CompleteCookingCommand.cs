using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.CompleteCooking
{
    public class CompleteCookingCommand : IRequest<Result<Guid>>
    {
        public Guid OrderItemId { get; set; }
    }
}

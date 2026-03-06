using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.MarkReady
{
    public class MarkReadyCommand : IRequest<Result<Guid>>
    {
        public Guid OrderItemId { get; set; }
    }
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Areas.Commands.DeactivateArea
{
    public sealed record DeactivateAreaCommand(Guid AreaId) : IRequest<Result<DeactivateAreaResponse>>;
}

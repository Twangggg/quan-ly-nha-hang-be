using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public record GetOptionGroupsByMenuItemQuery(Guid MenuItemId) : IRequest<Result<List<OptionGroupDto>>>;
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public record GetOptionGroupsByMenuItemQuery(Guid MenuItemId) : IRequest<Result<List<GetOptionGroupsResponse>>>;
}

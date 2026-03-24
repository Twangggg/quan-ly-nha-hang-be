using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Groups.Queries.GetInventoryGroups
{
    public sealed record GetInventoryGroupsQuery() : IRequest<Result<List<GetInventoryGroupsResponse>>>;
}

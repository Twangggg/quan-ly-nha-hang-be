using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;
using MediatR;

namespace FoodHub.Application.Features.Options.Queries.GetReusableOptionGroups
{
    public record GetReusableOptionGroupsQuery(int PageNumber = 1, int PageSize = 100)
        : IRequest<Result<PagedResult<OptionGroupResponse>>>;
}

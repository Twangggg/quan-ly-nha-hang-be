using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public record GetSetMenuByIdQuery(Guid SetMenuId) : IRequest<Result<GetSetMenuByIdResponse>>;
}

using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public record GetSetMenuByIdQuery(Guid SetMenuId) : IRequest<Result<SetMenuDetailDto>>;
}

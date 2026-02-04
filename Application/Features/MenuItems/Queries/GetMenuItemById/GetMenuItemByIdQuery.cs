using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public record GetMenuItemByIdQuery(Guid Id) : IRequest<Result<GetMenuItemByIdResponse>>;
}

using FoodHub.Application.Common.Models;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem
{
    public record DeleteMenuItemCommand(Guid MenuItemId) : IRequest<Result<DeleteMenuItemResponse>>;
}

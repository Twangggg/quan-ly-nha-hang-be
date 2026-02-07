using FoodHub.Application.Common.Models;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public record UpdateMenuItemCommand(
        Guid MenuItemId,
        string Name,
        string ImageUrl,
        string? Description,
        Guid CategoryId,
        Station Station,
        int ExpectedTime, 
        decimal PriceDineIn,
        decimal PriceTakeAway,
        decimal? CostPrice) : IRequest<Result<UpdateMenuItemResponse>>;
}

using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public record CreateMenuItemCommand(
        string Code,
        string Name,
        string? ImageUrl,
        string? Description,
        Guid CategoryId,
        Station Station,
        int ExpectedTime,
        decimal PriceDineIn,
        decimal? PriceTakeAway,
        decimal? Cost,
        IFormFile? ImageFile
    ) : IRequest<Result<CreateMenuItemResponse>>;

}

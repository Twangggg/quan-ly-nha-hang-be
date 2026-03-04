using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public record CreateMenuItemCommand(
        string Name,
        string? ImageUrl,
        string? Description,
        Guid CategoryId,
        int Station,
        int ExpectedTime,
        decimal Price,
        //IFormFile? ImageFile,
        decimal? Cost
    ) : IRequest<Result<CreateMenuItemResponse>>;

}

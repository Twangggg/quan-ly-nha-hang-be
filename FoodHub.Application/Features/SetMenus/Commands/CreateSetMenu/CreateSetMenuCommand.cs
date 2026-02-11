using FluentValidation;
using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public record CreateSetMenuCommand(
        string Code,
        string Name,
        SetType SetType,
        string? ImageUrl,
        string? Description,
        decimal Price,
        decimal CostPrice,
        //IFormFile? ImageFile,
        List<CreateSetMenuItemRequest> Items
    ) : IRequest<Result<CreateSetMenuResponse>>;

    public record CreateSetMenuItemRequest(Guid MenuItemId, int Quantity);
}

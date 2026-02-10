using FluentValidation;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu
{
    public record DeleteSetMenuCommand(Guid SetMenuId) : IRequest<Result<DeleteSetMenuResponse>>;
}


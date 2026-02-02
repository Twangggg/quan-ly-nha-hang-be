using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public record CreateOptionGroupCommand(
        Guid MenuItemId,
        string Name,
        int Type,
        bool IsRequired
    ) : IRequest<Result<OptionGroupDto>>;
}

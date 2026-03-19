using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public record UpdateOptionGroupCommand(
        Guid OptionGroupId,
        string Name,
        OptionGroupType Type,
        bool IsRequired
    ) : IRequest<Result<UpdateOptionGroupResponse>>;
}

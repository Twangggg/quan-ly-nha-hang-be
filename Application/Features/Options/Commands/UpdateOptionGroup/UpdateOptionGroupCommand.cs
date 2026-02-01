using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public record UpdateOptionGroupCommand(
        Guid OptionGroupId,
        string Name,
        int Type,
        bool IsRequired
    ) : IRequest<Result<OptionGroupDto>>;
}

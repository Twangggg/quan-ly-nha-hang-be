using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public record CreateOptionItemCommand(
        Guid OptionGroupId,
        string Label,
        decimal ExtraPrice
    ) : IRequest<Result<CreateOptionItemResponse>>;
}

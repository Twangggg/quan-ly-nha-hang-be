using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public record UpdateOptionItemCommand(
        Guid OptionItemId,
        string Label,
        decimal ExtraPrice
    ) : IRequest<Result<OptionItemDto>>;
}

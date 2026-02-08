using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public record UpdateOptionItemCommand(
        Guid OptionItemId,
        string Label,
        decimal ExtraPrice
    ) : IRequest<Result<UpdateOptionItemResponse>>;
}

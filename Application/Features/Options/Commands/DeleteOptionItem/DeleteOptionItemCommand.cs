using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionItem
{
    public sealed record DeleteOptionItemCommand(Guid OptionItemId) : IRequest<Result<DeleteOptionItemResponse>>;
}

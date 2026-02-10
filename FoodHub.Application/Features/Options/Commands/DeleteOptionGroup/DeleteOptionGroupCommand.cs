using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionGroup
{
    public sealed record DeleteOptionGroupCommand(Guid OptionGroupId) : IRequest<Result<DeleteOptionGroupResponse>>;
}

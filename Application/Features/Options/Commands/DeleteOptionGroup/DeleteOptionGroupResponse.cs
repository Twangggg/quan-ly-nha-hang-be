using System;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionGroup
{
    public record DeleteOptionGroupResponse(Guid OptionGroupId, DateTime? DeletedAt);
}

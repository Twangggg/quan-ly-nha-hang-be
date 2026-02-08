using System;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionItem
{
    public record DeleteOptionItemResponse(Guid OptionItemId, DateTime? DeletedAt);
}

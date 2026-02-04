using System;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemResponse
    {
        public Guid OptionItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
    }
}

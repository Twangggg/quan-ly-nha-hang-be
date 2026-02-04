using System;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupResponse
    {
        public Guid OptionGroupId { get; set; }
        public Guid MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public bool IsRequired { get; set; }
        public List<OptionItemResponse>? OptionItems { get; set; }
    }
}

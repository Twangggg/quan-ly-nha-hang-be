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

    public class OptionItemResponse
    {
        public Guid OptionItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
    }
}

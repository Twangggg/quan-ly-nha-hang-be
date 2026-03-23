namespace FoodHub.Application.Features.Options.Commands.UpdateMenuItemOptionGroup
{
    public class UpdateMenuItemOptionGroupResponse
    {
        public Guid MenuItemOptionGroupId { get; set; }
        public Guid MenuItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public bool IsRequired { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; }
    }
}

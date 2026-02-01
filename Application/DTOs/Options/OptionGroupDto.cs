namespace FoodHub.Application.DTOs.Options
{
    public class OptionGroupDto
    {
        public Guid OptionGroupId { get; set; }
        public Guid MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public bool IsRequired { get; set; }
        public List<OptionItemDto>? OptionItems { get; set; }
    }
}

namespace FoodHub.Application.DTOs.Options
{
    public class OptionItemDto
    {
        public Guid OptionItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
    }
}

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemResponse
    {
        public Guid OptionItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
    }
}

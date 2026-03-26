namespace FoodHub.Infrastructure.Services;

public class PayOsSettings
{
    public const string SectionName = "PayOS";

    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

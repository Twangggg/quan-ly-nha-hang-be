using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces.Branding
{
    public interface IBrandingSettingsProvider
    {
        Task<BrandingSettings> GetOrCreateAsync(CancellationToken cancellationToken = default);
    }
}

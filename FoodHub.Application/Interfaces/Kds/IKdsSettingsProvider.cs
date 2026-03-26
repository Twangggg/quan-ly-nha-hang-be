using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces.Kds
{
    public interface IKdsSettingsProvider
    {
        Task<KdsSettings> GetOrCreateAsync(CancellationToken cancellationToken = default);
    }
}

using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces.Reservations
{
    public interface IReservationSettingsProvider
    {
        Task<ReservationSettings> GetOrCreateAsync(CancellationToken cancellationToken = default);
    }
}

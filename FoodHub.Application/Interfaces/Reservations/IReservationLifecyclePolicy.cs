using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces.Reservations
{
    public interface IReservationLifecyclePolicy
    {
        DateTime GetBusinessNow();

        bool IsNoShowDue(Reservation reservation, ReservationSettings settings, DateTime now);

        bool IsBlockingReservation(
            Reservation reservation,
            ReservationSettings settings,
            DateTime now
        );
    }
}

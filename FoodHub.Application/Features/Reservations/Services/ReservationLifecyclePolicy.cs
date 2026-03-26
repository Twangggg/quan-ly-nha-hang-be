using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Reservations.Services
{
    public class ReservationLifecyclePolicy : IReservationLifecyclePolicy
    {
        public DateTime GetBusinessNow() => DateTime.UtcNow.AddHours(7);

        public bool IsNoShowDue(Reservation reservation, ReservationSettings settings, DateTime now)
        {
            ArgumentNullException.ThrowIfNull(reservation);
            ArgumentNullException.ThrowIfNull(settings);

            return reservation.CanMarkNoShow(now, settings);
        }

        public bool IsBlockingReservation(
            Reservation reservation,
            ReservationSettings settings,
            DateTime now
        )
        {
            ArgumentNullException.ThrowIfNull(reservation);
            ArgumentNullException.ThrowIfNull(settings);

            return reservation.Status switch
            {
                ReservationStatus.Booked => !IsNoShowDue(reservation, settings, now),
                ReservationStatus.CheckIn => true,
                _ => false,
            };
        }
    }
}

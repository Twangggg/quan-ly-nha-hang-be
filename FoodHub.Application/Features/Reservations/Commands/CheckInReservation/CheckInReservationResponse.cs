namespace FoodHub.Application.Features.Reservations.Commands.CheckInReservation
{
    public class CheckInReservationResponse
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
    }
}

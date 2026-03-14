using FluentValidation;

namespace FoodHub.Application.Features.Reservations.Commands.CheckInReservation
{
    public class CheckInReservationCommandValidator : AbstractValidator<CheckInReservationCommand>
    {
        public CheckInReservationCommandValidator()
        {
            RuleFor(x => x.ReservationId)
                .NotEmpty().WithMessage("Reservation ID is required.");
        }
    }
}

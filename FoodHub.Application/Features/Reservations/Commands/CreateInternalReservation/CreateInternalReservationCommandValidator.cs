using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation
{
    public class CreateInternalReservationCommandValidator : AbstractValidator<CreateInternalReservationCommand>
    {
        public CreateInternalReservationCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Reservation.NameRequired))
                .MaximumLength(100).WithMessage(messageService.GetMessage(MessageKeys.Reservation.NameMaxLength));

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Reservation.PhoneRequired))
                .Matches(@"^(0|84|\+84)(3|5|7|8|9)([0-9]{8})$")
                .WithMessage(messageService.GetMessage(MessageKeys.Reservation.PhoneInvalid));

            RuleFor(x => x.GuestCount)
                .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.Reservation.InvalidGuestCount));

            RuleFor(x => x.ReservationTime)
                .InclusiveBetween(new TimeSpan(9, 0, 0), new TimeSpan(20, 0, 0))
                .WithMessage(messageService.GetMessage(MessageKeys.Reservation.InvalidTime));

            RuleFor(x => x)
                .Must(x => IsInFuture(x.ReservationDate, x.ReservationTime))
                .WithMessage(messageService.GetMessage(MessageKeys.Common.DateNotInFuture))
                .Must(x => IsAtLeast45MinutesFromNow(x.ReservationDate, x.ReservationTime))
                .WithMessage(messageService.GetMessage(MessageKeys.Reservation.TimeTooSoon));
        }

        private bool IsInFuture(DateOnly date, TimeSpan time)
        {
            var now = DateTime.Now;
            var reservationDateTime = date.ToDateTime(TimeOnly.FromTimeSpan(time));

            return reservationDateTime > now;
        }

        private bool IsAtLeast45MinutesFromNow(DateOnly date, TimeSpan time)
        {
            var now = DateTime.Now;
            var reservationDateTime = date.ToDateTime(TimeOnly.FromTimeSpan(time));

            // Phải cách thời điểm hiện tại ít nhất 45 phút
            return reservationDateTime >= now.AddMinutes(45);
        }
    }
}

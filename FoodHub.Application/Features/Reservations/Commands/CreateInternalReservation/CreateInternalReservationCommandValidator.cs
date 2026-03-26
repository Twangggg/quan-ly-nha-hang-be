using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;

namespace FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation
{
    public class CreateInternalReservationCommandValidator : AbstractValidator<CreateInternalReservationCommand>
    {
        private readonly IMessageService _messageService;
        private readonly IReservationSettingsProvider _reservationSettingsProvider;

        public CreateInternalReservationCommandValidator(
            IMessageService messageService,
            IReservationSettingsProvider reservationSettingsProvider
        )
        {
            _messageService = messageService;
            _reservationSettingsProvider = reservationSettingsProvider;

            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Reservation.NameRequired))
                .MaximumLength(100).WithMessage(_messageService.GetMessage(MessageKeys.Reservation.NameMaxLength));

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Reservation.PhoneRequired))
                .Matches(@"^(0|84|\+84)(3|5|7|8|9)([0-9]{8})$")
                .WithMessage(_messageService.GetMessage(MessageKeys.Reservation.PhoneInvalid));

            RuleFor(x => x.GuestCount)
                .GreaterThan(0).WithMessage(_messageService.GetMessage(MessageKeys.Reservation.InvalidGuestCount));

            RuleFor(x => x)
                .Must(x => IsInFuture(x.ReservationDate, x.ReservationTime))
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.DateNotInFuture));

            RuleFor(x => x).CustomAsync(async (request, context, cancellationToken) =>
            {
                if (!IsInFuture(request.ReservationDate, request.ReservationTime))
                {
                    return;
                }

                var settings = await _reservationSettingsProvider.GetOrCreateAsync(cancellationToken);
                var reservationTime = TimeOnly.FromTimeSpan(request.ReservationTime);

                if (
                    !IsWithinOperatingHours(
                        reservationTime,
                        settings.OpenTime,
                        settings.CloseTime
                    )
                )
                {
                    context.AddFailure(
                        _messageService.GetMessage(MessageKeys.Reservation.OutsideOperatingHours)
                    );
                    return;
                }

                if (
                    settings.BreakEnabled
                    && IsBreakTime(reservationTime, settings.BreakStart, settings.BreakEnd)
                )
                {
                    context.AddFailure(
                        _messageService.GetMessage(
                            MessageKeys.Reservation.BreakTime,
                            settings.BreakStart.ToString("HH:mm"),
                            settings.BreakEnd.ToString("HH:mm")
                        )
                    );
                    return;
                }

                if (!IsAtLeastLeadTimeFromNow(
                        request.ReservationDate,
                        reservationTime,
                        settings.MinLeadTimeMinutes
                    ))
                {
                    context.AddFailure(
                        _messageService.GetMessage(
                            MessageKeys.Reservation.TimeTooSoon,
                            settings.MinLeadTimeMinutes
                        )
                    );
                }
            });
        }

        private bool IsInFuture(DateOnly date, TimeSpan time)
        {
            var now = DateTime.Now;
            var reservationDateTime = date.ToDateTime(TimeOnly.FromTimeSpan(time));

            return reservationDateTime > now;
        }

        private bool IsBreakTime(TimeOnly time, TimeOnly breakStart, TimeOnly breakEnd)
        {
            return time >= breakStart && time < breakEnd;
        }

        private bool IsWithinOperatingHours(TimeOnly time, TimeOnly openTime, TimeOnly closeTime)
        {
            return time >= openTime && time <= closeTime;
        }

        private bool IsAtLeastLeadTimeFromNow(DateOnly date, TimeOnly time, int leadTimeMinutes)
        {
            var now = DateTime.Now;
            var reservationDateTime = date.ToDateTime(time);

            return reservationDateTime >= now.AddMinutes(leadTimeMinutes);
        }
    }
}

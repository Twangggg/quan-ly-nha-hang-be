using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;

namespace FoodHub.Application.Features.Reservations.Commands.UpdateReservation
{
    public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
    {
        private readonly IMessageService _messageService;
        private readonly IReservationSettingsProvider _reservationSettingsProvider;

        public UpdateReservationCommandValidator(
            IMessageService messageService,
            IReservationSettingsProvider reservationSettingsProvider
        )
        {
            _messageService = messageService;
            _reservationSettingsProvider = reservationSettingsProvider;

            RuleFor(x => x.ReservationId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));

            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Reservation.NameRequired))
                .MaximumLength(100).WithMessage(messageService.GetMessage(MessageKeys.Reservation.NameMaxLength));

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Reservation.PhoneRequired))
                .Matches(@"^(0|84|\+84)(3|5|7|8|9)([0-9]{8})$")
                .WithMessage(messageService.GetMessage(MessageKeys.Reservation.PhoneInvalid));

            RuleFor(x => x.GuestCount)
                .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.Reservation.InvalidGuestCount));

            RuleFor(x => x.ReservationDate)
                .Must(date => date >= DateOnly.FromDateTime(DateTime.Now))
                .WithMessage(messageService.GetMessage(MessageKeys.Common.DateNotInFuture));

            RuleFor(x => x)
                .CustomAsync(
                    async (request, context, cancellationToken) =>
                    {
                        if (request.ReservationDate < DateOnly.FromDateTime(DateTime.Now))
                        {
                            context.AddFailure(
                                _messageService.GetMessage(MessageKeys.Reservation.InvalidTime)
                            );
                            return;
                        }

                        var settings = await _reservationSettingsProvider.GetOrCreateAsync(
                            cancellationToken
                        );
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
                                _messageService.GetMessage(
                                    MessageKeys.Reservation.OutsideOperatingHours
                                )
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

                        if (
                            !IsAtLeastLeadTimeFromNow(
                                request.ReservationDate,
                                reservationTime,
                                settings.MinLeadTimeMinutes
                            )
                        )
                        {
                            context.AddFailure(
                                _messageService.GetMessage(
                                    MessageKeys.Reservation.TimeTooSoon,
                                    settings.MinLeadTimeMinutes
                                )
                            );
                        }
                    }
                );
        }

        private static bool IsBreakTime(TimeOnly time, TimeOnly breakStart, TimeOnly breakEnd)
        {
            return time >= breakStart && time < breakEnd;
        }

        private static bool IsWithinOperatingHours(
            TimeOnly time,
            TimeOnly openTime,
            TimeOnly closeTime
        )
        {
            return time >= openTime && time <= closeTime;
        }

        private static bool IsAtLeastLeadTimeFromNow(
            DateOnly date,
            TimeOnly time,
            int leadTimeMinutes
        )
        {
            var now = DateTime.Now;
            var reservationDateTime = date.ToDateTime(time);

            return reservationDateTime >= now.AddMinutes(leadTimeMinutes);
        }
    }
}

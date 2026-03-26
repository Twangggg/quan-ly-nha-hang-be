using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
    {
        private readonly IMessageService _messageService;
        private readonly IReservationSettingsProvider _reservationSettingsProvider;

        public CreateReservationCommandValidator(
            IMessageService messageService,
            IReservationSettingsProvider reservationSettingsProvider
        )
        {
            _messageService = messageService;
            _reservationSettingsProvider = reservationSettingsProvider;

            RuleFor(x => x.CustomerName)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Profile.FullNameRequired))
                .MaximumLength(100)
                .WithMessage(_messageService.GetMessage(MessageKeys.Profile.FullNameMaxLength));

            RuleFor(x => x.CustomerPhone)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Profile.PhoneRequired))
                .Matches("^(0|84|\\+84)(3|5|7|8|9)([0-9]{8})$")
                .WithMessage(_messageService.GetMessage(MessageKeys.Profile.PhoneInvalid));

            RuleFor(x => x.GuestCount)
                .GreaterThan(0)
                .WithMessage(_messageService.GetMessage(MessageKeys.Order.InvalidQuantity));

            RuleFor(x => x.AreaId)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.IdRequired));

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

        private bool IsValidReservationDate(DateOnly date)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            return date >= today;
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

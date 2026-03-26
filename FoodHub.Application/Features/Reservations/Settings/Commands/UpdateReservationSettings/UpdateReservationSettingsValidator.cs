using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using System.Globalization;

namespace FoodHub.Application.Features.Reservations.Settings.Commands.UpdateReservationSettings
{
    public class UpdateReservationSettingsValidator
        : AbstractValidator<UpdateReservationSettingsCommand>
    {
        public UpdateReservationSettingsValidator(IMessageService messageService)
        {
            RuleFor(x => x.OpenTime)
                .NotEmpty()
                .Must(BeValidTime)
                .WithMessage(messageService.GetMessage(MessageKeys.ReservationSettings.InvalidOperatingHours));

            RuleFor(x => x.CloseTime)
                .NotEmpty()
                .Must(BeValidTime)
                .WithMessage(messageService.GetMessage(MessageKeys.ReservationSettings.InvalidOperatingHours));

            RuleFor(x => x)
                .Must(x =>
                    TryParseTime(x.OpenTime, out var openTime)
                    && TryParseTime(x.CloseTime, out var closeTime)
                    && openTime < closeTime
                )
                .WithMessage(messageService.GetMessage(MessageKeys.ReservationSettings.InvalidOperatingHours));

            When(x => x.BreakEnabled, () =>
            {
                RuleFor(x => x.BreakStart)
                    .NotEmpty()
                    .Must(BeValidTime)
                    .WithMessage(
                        messageService.GetMessage(
                            MessageKeys.ReservationSettings.InvalidBreakHours
                        )
                    );

                RuleFor(x => x.BreakEnd)
                    .NotEmpty()
                    .Must(BeValidTime)
                    .WithMessage(
                        messageService.GetMessage(
                            MessageKeys.ReservationSettings.InvalidBreakHours
                        )
                    );

                RuleFor(x => x)
                    .Must(x =>
                    {
                        if (!TryParseTime(x.BreakStart, out var breakStart))
                        {
                            return false;
                        }

                        if (!TryParseTime(x.BreakEnd, out var breakEnd))
                        {
                            return false;
                        }

                        return breakStart < breakEnd;
                    })
                    .WithMessage(
                        messageService.GetMessage(
                            MessageKeys.ReservationSettings.InvalidBreakHours
                        )
                    );

                RuleFor(x => x)
                    .Must(x =>
                    {
                        if (!TryParseTime(x.OpenTime, out var openTime))
                        {
                            return false;
                        }

                        if (!TryParseTime(x.CloseTime, out var closeTime))
                        {
                            return false;
                        }

                        if (!TryParseTime(x.BreakStart, out var breakStart))
                        {
                            return false;
                        }

                        if (!TryParseTime(x.BreakEnd, out var breakEnd))
                        {
                            return false;
                        }

                        return breakStart >= openTime && breakEnd <= closeTime;
                    })
                    .WithMessage(
                        messageService.GetMessage(
                            MessageKeys.ReservationSettings.InvalidBreakOutsideOperatingHours
                        )
                    );
            });

            RuleFor(x => x.OverlapBufferMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    messageService.GetMessage(
                        MessageKeys.ReservationSettings.InvalidOverlapBufferMinutes
                    )
                );

            RuleFor(x => x.MinLeadTimeMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    messageService.GetMessage(
                        MessageKeys.ReservationSettings.InvalidMinLeadTimeMinutes
                    )
                );

            RuleFor(x => x.GracePeriodMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    messageService.GetMessage(
                        MessageKeys.ReservationSettings.InvalidGracePeriodMinutes
                    )
                );

        }

        private static bool BeValidTime(string value) => TryParseTime(value, out _);

        private static bool TryParseTime(string value, out TimeOnly time)
        {
            return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
        }
    }
}

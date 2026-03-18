using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
    {
        private readonly IMessageService _messageService;

        public CreateReservationCommandValidator(IMessageService messageService)
        {
            _messageService = messageService;

            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Profile.FullNameRequired))
                .MaximumLength(100).WithMessage(_messageService.GetMessage(MessageKeys.Profile.FullNameMaxLength));

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Profile.PhoneRequired))
                .MaximumLength(20).WithMessage(_messageService.GetMessage(MessageKeys.Profile.PhoneInvalid));

            RuleFor(x => x.GuestCount)
                .GreaterThan(0).WithMessage(_messageService.GetMessage(MessageKeys.Order.InvalidQuantity));

            RuleFor(x => x.PartyType)
                .IsInEnum().WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidFormat));

            RuleFor(x => x.AreaId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Common.IdRequired));

            // AC-PR-01 & AC-PR-02: Validate time constraints
            RuleFor(x => x)
                .Must(x => IsValidReservationTime(x.ReservationDate, x.ReservationTime))
                .WithMessage(_messageService.GetMessage(MessageKeys.Reservation.InvalidTime))
                .Must(x => x.ReservationTime >= new TimeSpan(9, 0, 0) && x.ReservationTime <= new TimeSpan(20, 0, 0))
                .WithMessage(_messageService.GetMessage(MessageKeys.Reservation.InvalidTime));
        }

        private bool IsValidReservationTime(DateOnly date, TimeSpan time)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            if (date < today) return false;

            if (date == today)
            {
                var minTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(45));
                if (time < minTime) return false;
            }

            return true;
        }
    }
}

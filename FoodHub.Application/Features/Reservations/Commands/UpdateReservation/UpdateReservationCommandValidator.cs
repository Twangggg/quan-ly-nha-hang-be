using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using System;

namespace FoodHub.Application.Features.Reservations.Commands.UpdateReservation
{
    public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>  
    {
        public UpdateReservationCommandValidator(IMessageService messageService) {
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
        }
    }
}

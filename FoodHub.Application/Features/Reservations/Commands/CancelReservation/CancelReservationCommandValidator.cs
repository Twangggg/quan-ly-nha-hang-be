using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Reservations.Commands.CancelReservation
{
    public class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
    {
        public CancelReservationCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.ReservationId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}

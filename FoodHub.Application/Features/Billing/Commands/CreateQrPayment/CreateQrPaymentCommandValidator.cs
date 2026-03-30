using FluentValidation;

using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Billing.Commands.CreateQrPayment
{
    public class CreateQrPaymentCommandValidator : AbstractValidator<CreateQrPaymentCommand>
    {
        public CreateQrPaymentCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "OrderId" }));
        }
    }
}

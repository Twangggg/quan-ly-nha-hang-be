using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.PaymentMethods.Commands.CreatePaymentMethod
{
    public class CreatePaymentMethodValidator : AbstractValidator<CreatePaymentMethodCommand>
    {
        public CreatePaymentMethodValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(MessageKeys.PaymentMethodConfig.NameRequired)
                .MaximumLength(100).WithMessage(MessageKeys.PaymentMethodConfig.NameMaxLength);

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage(MessageKeys.PaymentMethodConfig.TypeRequired);


        }
    }
}

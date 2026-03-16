using FluentValidation;

namespace FoodHub.Application.Features.Invoices.Commands.CreateInvoice
{
    public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
    {
        public CreateInvoiceValidator()
        {
            RuleFor(i => i.OrderId)
                .NotEmpty().WithMessage("OrderId is required.");
            RuleFor(i => i.AmountReceived)
                .GreaterThanOrEqualTo(0).WithMessage("AmountReceived must be greater than or equal to 0.");
        }
    }
}

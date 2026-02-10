using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen
{
    public class SubmitOrderToKitchenValidator : AbstractValidator<SubmitOrderToKitchenCommand>
    {
        public SubmitOrderToKitchenValidator(IMessageService messageService)
        {
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity));
            RuleFor(x => x.TableId)
                .NotNull()
                .When(x => x.OrderType == Domain.Enums.OrderType.DineIn)
                .WithMessage(messageService.GetMessage(MessageKeys.Order.SelectTable));
            // Validation for SelectedOptions
            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    // Existing rules...
                    item.When(i => i.SelectedOptions != null && i.SelectedOptions.Any(), () =>
                    {
                        item.RuleForEach(i => i.SelectedOptions)
                            .ChildRules(optionGroup =>
                            {
                                optionGroup.RuleFor(og => og.SelectedValues)
                                    .NotEmpty()
                                    .WithMessage("Option group must have at least one selected value");

                                optionGroup.RuleForEach(og => og.SelectedValues)
                                    .ChildRules(value =>
                                    {
                                        value.RuleFor(v => v.Quantity)
                                            .GreaterThan(0)
                                            .WithMessage("Option quantity must be greater than 0");
                                    });
                            });
                    });
                });

        }
    }
}

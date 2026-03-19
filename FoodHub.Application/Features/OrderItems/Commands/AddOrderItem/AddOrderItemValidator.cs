using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemValidator : AbstractValidator<AddOrderItemCommand>
    {
        public AddOrderItemValidator(IMessageService message)
        {
            RuleFor(o => o.OrderId).NotEmpty().WithMessage(message.GetMessage(MessageKeys.Order.NotFound));
            RuleFor(o => o.MenuItemId).NotEmpty().WithMessage(message.GetMessage(MessageKeys.MenuItem.NotFound));
            RuleFor(o => o.Quantity).GreaterThan(0).WithMessage(message.GetMessage(MessageKeys.Order.InvalidQuantity));
            RuleFor(x => x.SelectedOptions)
                .Must(options => options == null || options.Select(o => o.OptionGroupId).Distinct().Count() == options.Count)
                .WithMessage("Duplicate option groups are not allowed.");

            RuleForEach(x => x.SelectedOptions)
                .ChildRules(optionGroup =>
                {
                    optionGroup.RuleFor(og => og.SelectedValues)
                        .Must(values => values.Count > 0)
                        .When(og => og.SelectedValues != null)
                        .WithMessage("Option group must have at least one selected value.");

                    optionGroup.RuleForEach(og => og.SelectedValues)
                        .ChildRules(value =>
                        {
                            value.RuleFor(v => v.Quantity)
                                .GreaterThan(0)
                                .WithMessage("Option quantity must be greater than 0.");
                        });
                });
        }
    }
}

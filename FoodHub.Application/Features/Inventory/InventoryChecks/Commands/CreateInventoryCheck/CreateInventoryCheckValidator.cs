using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck
{
    public class CreateInventoryCheckValidator : AbstractValidator<CreateInventoryCheckCommand>
    {
        public CreateInventoryCheckValidator(IMessageService messageService)
        {
            RuleFor(x => x.CheckDate)
                .LessThanOrEqualTo(_ => DateTime.UtcNow)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.DateNotInFuture));

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.InventoryCheck.ItemsRequired))
                .Must(items => items.Select(x => x.IngredientId).Distinct().Count() == items.Count)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.InventoryCheck.DuplicateIngredient)
                );

            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(x => x.IngredientId)
                        .NotEmpty()
                        .WithMessage(
                            messageService.GetMessage(
                                MessageKeys.InventoryCheck.IngredientIdRequired
                            )
                        );

                    item.RuleFor(x => x.PhysicalQuantity)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.InventoryCheck.QuantityMin)
                        );

                    item.RuleFor(x => x.Reason)
                        .MaximumLength(500)
                        .When(x => !string.IsNullOrWhiteSpace(x.Reason))
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.InventoryCheck.ReasonMaxLength)
                        );
                });
        }
    }
}

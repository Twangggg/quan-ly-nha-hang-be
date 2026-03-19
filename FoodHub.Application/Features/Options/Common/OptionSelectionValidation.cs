using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Options.Common
{
    public record RequestedOptionSelection(
        Guid OptionGroupId,
        IReadOnlyCollection<RequestedOptionValue> SelectedValues
    );

    public record RequestedOptionValue(Guid OptionItemId, int Quantity, string? Note);

    public record ValidatedOptionSelection(
        MenuItemOptionGroup Assignment,
        OptionGroup Group,
        List<(OptionItem Item, int Quantity, string? Note)> Selections
    );

    public static class OptionSelectionValidation
    {
        public static Result<List<ValidatedOptionSelection>> ValidateForMenuItem(
            MenuItem menuItem,
            IReadOnlyCollection<RequestedOptionSelection>? requestedSelections,
            IMessageService messageService
        )
        {
            var assignments = menuItem.MenuItemOptionGroups
                .Where(x => x.DeletedAt == null && x.IsVisible)
                .ToDictionary(x => x.OptionGroupId);
            var requests =
                requestedSelections?.ToDictionary(x => x.OptionGroupId)
                ?? new Dictionary<Guid, RequestedOptionSelection>();

            foreach (var requestedGroupId in requests.Keys)
            {
                if (!assignments.ContainsKey(requestedGroupId))
                {
                    return Result<List<ValidatedOptionSelection>>.Failure(
                        $"Option group {requestedGroupId} is not assigned to menu item {menuItem.MenuItemId}.",
                        ResultErrorType.BadRequest
                    );
                }
            }

            var validatedSelections = new List<ValidatedOptionSelection>();

            foreach (var assignment in assignments.Values)
            {
                requests.TryGetValue(assignment.OptionGroupId, out var requestedSelection);

                var selectedValues = requestedSelection?.SelectedValues ?? Array.Empty<RequestedOptionValue>();
                var selectedQuantity = selectedValues.Sum(x => x.Quantity);

                if (assignment.IsRequired && selectedQuantity == 0)
                {
                    return Result<List<ValidatedOptionSelection>>.Failure(
                        $"Option group '{assignment.OptionGroup.Name}' is required.",
                        ResultErrorType.BadRequest
                    );
                }

                if (selectedQuantity < assignment.MinSelect)
                {
                    return Result<List<ValidatedOptionSelection>>.Failure(
                        $"Option group '{assignment.OptionGroup.Name}' requires at least {assignment.MinSelect} selection(s).",
                        ResultErrorType.BadRequest
                    );
                }

                if (selectedQuantity > assignment.MaxSelect)
                {
                    return Result<List<ValidatedOptionSelection>>.Failure(
                        $"Option group '{assignment.OptionGroup.Name}' allows at most {assignment.MaxSelect} selection(s).",
                        ResultErrorType.BadRequest
                    );
                }

                var itemsById = assignment.OptionGroup.OptionItems.ToDictionary(x => x.OptionItemId);
                var domainSelections = new List<(OptionItem Item, int Quantity, string? Note)>();

                foreach (var value in selectedValues)
                {
                    if (value.Quantity <= 0)
                    {
                        return Result<List<ValidatedOptionSelection>>.Failure(
                            messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity),
                            ResultErrorType.BadRequest
                        );
                    }

                    if (!itemsById.TryGetValue(value.OptionItemId, out var item))
                    {
                        return Result<List<ValidatedOptionSelection>>.Failure(
                            $"Option item {value.OptionItemId} does not belong to option group {assignment.OptionGroupId}.",
                            ResultErrorType.BadRequest
                        );
                    }

                    domainSelections.Add((item, value.Quantity, value.Note));
                }

                if (domainSelections.Count > 0)
                {
                    validatedSelections.Add(
                        new ValidatedOptionSelection(
                            assignment,
                            assignment.OptionGroup,
                            domainSelections
                        )
                    );
                }
            }

            return Result<List<ValidatedOptionSelection>>.Success(validatedSelections);
        }
    }
}

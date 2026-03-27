using FoodHub.Application.Features.KDS.Queries.GetKdsItems;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.KDS.Common
{
    public static class KdsMappingHelper
    {
        public static KdsItemResponse MapToResponse(
            OrderItem oi,
            KdsPriorityCalculator priorityCalculator,
            KdsSettings settings
        )
        {
            var orderType = oi.Order?.OrderType ?? OrderType.DineIn;
            var isOrderPriority = oi.Order?.IsPriority ?? false;

            // Note: These might be slightly inaccurate if not all items are loaded,
            // but for a single notification it's usually acceptable or we fetch them.
            var totalOrderItems = oi.Order?.OrderItems?.Count ?? 0;
            var finishedOrderItems =
                oi.Order?.OrderItems?.Count(x => x.Status == OrderItemStatus.Completed) ?? 0;

            var expectedTimeSeconds = (oi.MenuItem?.ExpectedTime ?? 0) * 60;

            return new KdsItemResponse
            {
                OrderItemId = oi.OrderItemId,
                OrderId = oi.OrderId,
                OrderCode = oi.Order?.OrderCode ?? string.Empty,
                ItemNameSnapshot = oi.ItemNameSnapshot,
                StationSnapshot = oi.StationSnapshot,
                Quantity = oi.Quantity,
                ItemNote = oi.ItemNote,
                Status = oi.Status.ToString(),
                RejectionReason = oi.RejectionReason,
                CreatedAt = oi.CreatedAt,
                IsOrderPriority = isOrderPriority,
                IsPriority = isOrderPriority,
                OrderType = orderType.ToString(),
                TotalOrderItems = totalOrderItems,
                FinishedOrderItems = finishedOrderItems,
                ExpectedTimeSeconds = expectedTimeSeconds,
                PriorityScore = priorityCalculator.Calculate(
                    settings,
                    oi.CreatedAt,
                    isOrderPriority,
                    expectedTimeSeconds,
                    orderType,
                    totalOrderItems,
                    finishedOrderItems
                ),
                ItemOptions = string.Join(
                    ", ",
                    (oi.OptionGroups ?? Enumerable.Empty<OrderItemOptionGroup>())
                        .SelectMany(g => g.OptionValues ?? Enumerable.Empty<OrderItemOptionValue>())
                        .Select(v =>
                            v.Quantity > 1 ? $"{v.LabelSnapshot} x{v.Quantity}" : v.LabelSnapshot
                        )
                ),
                OptionGroups = oi.OptionGroups?.ToList() ?? new List<OrderItemOptionGroup>(),
            };
        }
    }
}

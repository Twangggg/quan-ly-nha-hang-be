using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.KDS.Common
{
    public class KdsPriorityCalculator
    {
        public int Calculate(
            KdsSettings settings,
            DateTime createdAt,
            bool isOrderPriority,
            int expectedTimeSeconds,
            OrderType orderType,
            int totalOrderItems,
            int finishedOrderItems
        )
        {
            var score = 0.0;
            var now = DateTime.UtcNow;

            var waitMinutes = (now - createdAt).TotalMinutes;
            score += waitMinutes * settings.WaitTimePerMinute;

            if (isOrderPriority)
            {
                score += settings.OrderPriorityBonus;
            }

            var expectedTimeMinutes = expectedTimeSeconds / 60.0;
            score += expectedTimeMinutes * settings.ExpectedTimeWeight;

            if (waitMinutes > expectedTimeMinutes)
            {
                var overdueMinutes = waitMinutes - expectedTimeMinutes;
                score += overdueMinutes * settings.OverduePerMinute;
            }

            if (totalOrderItems > 0)
            {
                score +=
                    ((double)finishedOrderItems / totalOrderItems) * settings.CompletionBoostWeight;
            }

            score += orderType switch
            {
                OrderType.Takeaway => settings.TakeawayBonus,
                OrderType.Delivery => settings.DeliveryBonus,
                _ => 0,
            };

            return (int)Math.Round(score);
        }

        public List<T> SortQueue<T>(
            IEnumerable<T> items,
            KdsSortMode sortMode,
            Func<T, int> prioritySelector,
            Func<T, DateTime> createdAtSelector
        )
        {
            return sortMode switch
            {
                KdsSortMode.Fifo => items.OrderBy(createdAtSelector).ToList(),
                _ => items.OrderByDescending(prioritySelector).ThenBy(createdAtSelector).ToList(),
            };
        }

        public List<T> SortActiveItems<T>(
            IEnumerable<T> items,
            KdsSortMode sortMode,
            Func<T, bool> isCookingSelector,
            Func<T, int> prioritySelector,
            Func<T, DateTime> createdAtSelector
        )
        {
            return sortMode switch
            {
                KdsSortMode.Fifo => items
                    .OrderBy(x => isCookingSelector(x) ? 0 : 1)
                    .ThenBy(createdAtSelector)
                    .ToList(),
                _ => items
                    .OrderBy(x => isCookingSelector(x) ? 0 : 1)
                    .ThenByDescending(prioritySelector)
                    .ThenBy(createdAtSelector)
                    .ToList(),
            };
        }
    }
}

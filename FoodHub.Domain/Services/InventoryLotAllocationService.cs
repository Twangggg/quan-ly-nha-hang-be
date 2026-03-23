using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;

namespace FoodHub.Domain.Services
{
    public class InventoryLotAllocationService
    {
        public DomainResult<IReadOnlyList<InventoryLotAllocationPlan>> Allocate(
            IEnumerable<InventoryLot> lots,
            decimal quantityToAllocate,
            DateTime occurredAt
        )
        {
            if (quantityToAllocate <= 0)
            {
                return DomainResult<IReadOnlyList<InventoryLotAllocationPlan>>.Failure(
                    DomainErrors.InventoryLot.InvalidQuantity
                );
            }

            var orderedLots = lots
                .Where(x => x.GetAvailableQuantity(occurredAt) > 0)
                .OrderBy(x => x.ExpiryDate.HasValue ? 0 : 1)
                .ThenBy(x => x.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(x => x.ReceivedAt)
                .ThenBy(x => x.CreatedAt)
                .ToList();

            var remaining = quantityToAllocate;
            var plans = new List<InventoryLotAllocationPlan>();

            foreach (var lot in orderedLots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var allocatedQuantity = Math.Min(lot.GetAvailableQuantity(occurredAt), remaining);
                if (allocatedQuantity <= 0)
                {
                    continue;
                }

                plans.Add(new InventoryLotAllocationPlan(lot, allocatedQuantity));
                remaining -= allocatedQuantity;
            }

            if (remaining > 0)
            {
                return DomainResult<IReadOnlyList<InventoryLotAllocationPlan>>.Failure(
                    DomainErrors.InventoryLot.InsufficientQuantity
                );
            }

            return DomainResult<IReadOnlyList<InventoryLotAllocationPlan>>.Success(plans);
        }
    }

    public sealed record InventoryLotAllocationPlan(InventoryLot Lot, decimal Quantity);
}

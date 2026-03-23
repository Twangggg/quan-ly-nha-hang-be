using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Lots.Commands.DisposeInventoryLot
{
    public class DisposeInventoryLotResponse
    {
        public Guid LotId { get; set; }
        public decimal RemainingQuantity { get; set; }
        public InventoryLotStatus Status { get; set; }
    }
}

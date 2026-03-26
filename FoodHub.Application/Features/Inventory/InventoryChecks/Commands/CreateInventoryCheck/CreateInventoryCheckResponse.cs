using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck
{
    public class CreateInventoryCheckResponse
    {
        public Guid InventoryCheckId { get; set; }
        public DateTime CheckDate { get; set; }
        public InventoryCheckStatus Status { get; set; }
        public int TotalItems { get; set; }
    }
}

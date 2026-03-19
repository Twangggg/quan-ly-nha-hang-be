using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryChecks
{
    public class GetInventoryChecksResponse
    {
        public Guid InventoryCheckId { get; set; }
        public DateTime CheckDate { get; set; }
        public InventoryCheckStatus Status { get; set; }
        public string? CreatedByName { get; set; }
        public int TotalItems { get; set; }
    }
}

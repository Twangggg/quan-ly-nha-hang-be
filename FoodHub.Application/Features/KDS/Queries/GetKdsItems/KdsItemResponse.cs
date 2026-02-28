namespace FoodHub.Application.Features.KDS.Queries.GetKdsItems
{
    public class KdsItemResponse
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public string StationSnapshot { get; set; } = null!;
        public int Quantity { get; set; }
        public string? ItemNote { get; set; }
        public string Status { get; set; } = null!;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

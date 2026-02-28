namespace FoodHub.Application.Features.KDS.Queries.GetKdsQueue
{
    public class KdsQueueResponse
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public string StationSnapshot { get; set; } = null!;
        public int Quantity { get; set; }
        public string? ItemNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public int QueuePosition { get; set; }
    }
}

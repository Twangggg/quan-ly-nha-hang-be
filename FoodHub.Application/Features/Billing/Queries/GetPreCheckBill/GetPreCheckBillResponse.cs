namespace FoodHub.Application.Features.Billing.Queries.GetPreCheckBill
{
    public class GetPreCheckBillResponse
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public int? TableNumber { get; set; }
        public string EmployeeName { get; set; } = null!;
        public DateTime PrintedAt { get; set; }
        public List<PreCheckBillItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Vat { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class PreCheckBillItemDto
    {
        public string ItemName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? OptionsSummary { get; set; }
        public decimal LineTotal { get; set; }
    }
}

namespace FoodHub.Application.Features.Billing.Queries.GetRevenueByPaymentMethod
{
    public class GetRevenueByPaymentMethodResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public List<RevenueByMethodDto> Items { get; set; } = new();
    }

    public class RevenueByMethodDto
    {
        public Guid PaymentMethodConfigId { get; set; }
        public string PaymentMethodName { get; set; } = null!;
        public string PaymentMethodType { get; set; } = null!;
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }
}

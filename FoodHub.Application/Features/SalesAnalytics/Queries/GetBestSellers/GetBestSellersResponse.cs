namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers
{
    public class BestSellerDto
    {
        public string ItemName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double RevenuePercentage { get; set; }
        public decimal GrossProfit { get; set; }
    }

    public class GetBestSellersResponse
    {
        public List<BestSellerDto> Items { get; set; } = new();
    }
}

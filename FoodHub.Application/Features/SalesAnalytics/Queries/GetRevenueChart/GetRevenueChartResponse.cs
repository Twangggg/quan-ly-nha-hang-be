namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetRevenueChart
{
    public class GetRevenueChartResponse
    {
        public List<RevenuePointDto> Points { get; set; } = new();
    }

    public class RevenuePointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}

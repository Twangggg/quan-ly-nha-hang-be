namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport
{
    public class GetCategoryReportResponse
    {
        public List<CategoryReportDto> Items { get; set; } = new();
    }

    public class CategoryReportDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public double RevenuePercentage { get; set; }
        public int ItemCount { get; set; }
    }
}

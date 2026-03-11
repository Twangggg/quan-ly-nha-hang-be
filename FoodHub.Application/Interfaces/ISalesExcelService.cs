using FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetMonthlyReport;

namespace FoodHub.Application.Interfaces
{
    public interface ISalesExcelService
    {
        byte[] ExportAnalyticsToExcel(
            string reportTitle,
            GetDailyReportResponse? summary,
            List<BestSellerDto> bestSellers,
            List<CategoryReportDto> categories
        );
    }
}

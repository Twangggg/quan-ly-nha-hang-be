using ClosedXML.Excel;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport;
using FoodHub.Application.Interfaces;

namespace FoodHub.Infrastructure.Services
{
    public class SalesExcelService : ISalesExcelService
    {
        public byte[] ExportAnalyticsToExcel(
            string reportTitle,
            GetDailyReportResponse? summary,
            List<BestSellerDto> bestSellers,
            List<CategoryReportDto> categories
        )
        {
            using var workbook = new XLWorkbook();

            // Sheet 1: Tổng quan
            var summarySheet = workbook.Worksheets.Add("Tổng quan");
            AddSummarySheet(summarySheet, reportTitle, summary);

            // Sheet 2: Món bán chạy
            var bestSellersSheet = workbook.Worksheets.Add("Món bán chạy");
            AddBestSellersSheet(bestSellersSheet, bestSellers);

            // Sheet 3: Danh mục
            var categoriesSheet = workbook.Worksheets.Add("Theo danh mục");
            AddCategoriesSheet(categoriesSheet, categories);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private void AddSummarySheet(
            IXLWorksheet sheet,
            string title,
            GetDailyReportResponse? summary
        )
        {
            sheet.Cell(1, 1).Value = title;
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;
            sheet.Range(1, 1, 1, 3).Merge();

            if (summary == null)
                return;

            sheet.Cell(3, 1).Value = "Chỉ số";
            sheet.Cell(3, 2).Value = "Giá trị";
            sheet.Range(3, 1, 3, 2).Style.Font.Bold = true;
            sheet.Range(3, 1, 3, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

            sheet.Cell(4, 1).Value = "Tổng doanh thu";
            sheet.Cell(4, 2).Value = summary.TotalRevenue;
            sheet.Cell(4, 2).Style.NumberFormat.Format = "#,##0";

            sheet.Cell(5, 1).Value = "Tổng đơn hàng";
            sheet.Cell(5, 2).Value = summary.TotalOrders;

            sheet.Cell(6, 1).Value = "Đơn bị hủy";
            sheet.Cell(6, 2).Value = summary.CancelledOrders;

            if (summary.DailyTarget.HasValue)
            {
                sheet.Cell(7, 1).Value = "Mục tiêu";
                sheet.Cell(7, 2).Value = summary.DailyTarget.Value;
                sheet.Cell(7, 2).Style.NumberFormat.Format = "#,##0";
            }

            sheet.Columns().AdjustToContents();
        }

        private void AddBestSellersSheet(IXLWorksheet sheet, List<BestSellerDto> items)
        {
            sheet.Cell(1, 1).Value = "Tên món";
            sheet.Cell(1, 2).Value = "Danh mục";
            sheet.Cell(1, 3).Value = "Số lượng";
            sheet.Cell(1, 4).Value = "Doanh thu";
            sheet.Cell(1, 5).Value = "Lợi nhuận gộp";
            sheet.Cell(1, 6).Value = "% Doanh thu";

            sheet.Range(1, 1, 1, 6).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                sheet.Cell(row, 1).Value = items[i].ItemName;
                sheet.Cell(row, 2).Value = items[i].CategoryName;
                sheet.Cell(row, 3).Value = items[i].QuantitySold;
                sheet.Cell(row, 4).Value = items[i].TotalRevenue;
                sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(row, 5).Value = items[i].GrossProfit;
                sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(row, 6).Value = items[i].RevenuePercentage / 100;
                sheet.Cell(row, 6).Style.NumberFormat.Format = "0.0%";
            }

            sheet.Columns().AdjustToContents();
        }

        private void AddCategoriesSheet(IXLWorksheet sheet, List<CategoryReportDto> items)
        {
            sheet.Cell(1, 1).Value = "Tên danh mục";
            sheet.Cell(1, 2).Value = "Số lượng món";
            sheet.Cell(1, 3).Value = "Doanh thu";
            sheet.Cell(1, 4).Value = "% Doanh thu";

            sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                sheet.Cell(row, 1).Value = items[i].CategoryName;
                sheet.Cell(row, 2).Value = items[i].ItemCount;
                sheet.Cell(row, 3).Value = items[i].TotalRevenue;
                sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(row, 4).Value = items[i].RevenuePercentage / 100;
                sheet.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            }

            sheet.Columns().AdjustToContents();
        }
    }
}

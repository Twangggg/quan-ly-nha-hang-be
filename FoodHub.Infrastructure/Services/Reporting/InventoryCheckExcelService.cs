using ClosedXML.Excel;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.ExportInventoryCheck;
using FoodHub.Application.Interfaces.Reporting;
using System.Linq;

namespace FoodHub.Infrastructure.Services.Reporting;

public class InventoryCheckExcelService : IInventoryCheckExcelService
{
    public byte[] ExportInventoryCheckToExcel(ExportInventoryCheckResponse response)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Phieu_Kiem_Kho");

        sheet.Cell(1, 1).Value = "PHIẾU KIỂM KHO";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 7).Merge();

        sheet.Cell(2, 1).Value = $"Mã phiếu: {response.InventoryCheckId}";
        sheet.Cell(3, 1).Value = $"Ngày kiểm kê: {response.CheckDate:dd/MM/yyyy}";
        sheet.Cell(4, 1).Value = $"Trạng thái: {response.Status}";
        sheet.Cell(5, 1).Value = $"Ngày tạo: {response.CreatedAt:dd/MM/yyyy HH:mm}";
        sheet.Cell(6, 1).Value = $"Số mặt hàng: {response.TotalItems}";

        var headerRow = 8;
        sheet.Cell(headerRow, 1).Value = "STT";
        sheet.Cell(headerRow, 2).Value = "Mã nguyên liệu";
        sheet.Cell(headerRow, 3).Value = "Tên nguyên liệu";
        sheet.Cell(headerRow, 4).Value = "Đơn vị";
        sheet.Cell(headerRow, 5).Value = "Tồn theo sổ";
        sheet.Cell(headerRow, 6).Value = "Tồn thực tế";
        sheet.Cell(headerRow, 7).Value = "Chênh lệch";
        sheet.Cell(headerRow, 8).Value = "Giá trị sổ";
        sheet.Cell(headerRow, 9).Value = "Giá trị thực tế";
        sheet.Cell(headerRow, 10).Value = "Chênh lệch giá trị";
        sheet.Cell(headerRow, 11).Value = "Ghi chú";

        sheet.Range(headerRow, 1, headerRow, 11).Style.Font.Bold = true;
        sheet.Range(headerRow, 1, headerRow, 11).Style.Fill.BackgroundColor = XLColor.LightBlue;

        var currentRow = headerRow + 1;
        for (int i = 0; i < response.Items.Count; i++)
        {
            var item = response.Items[i];
            sheet.Cell(currentRow, 1).Value = i + 1;
            sheet.Cell(currentRow, 2).Value = item.IngredientCode;
            sheet.Cell(currentRow, 3).Value = item.IngredientName;
            sheet.Cell(currentRow, 4).Value = item.Unit;
            sheet.Cell(currentRow, 5).Value = item.BookQuantity;
            sheet.Cell(currentRow, 6).Value = item.PhysicalQuantity;
            sheet.Cell(currentRow, 7).Value = item.DifferenceQuantity;
            sheet.Cell(currentRow, 7).Style.Font.FontColor = item.DifferenceQuantity != 0 ? XLColor.Red : XLColor.Black;
            sheet.Cell(currentRow, 8).Value = item.BookValue;
            sheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(currentRow, 9).Value = item.PhysicalValue;
            sheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(currentRow, 10).Value = item.DifferenceValue;
            sheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(currentRow, 10).Style.Font.FontColor = item.DifferenceValue != 0 ? XLColor.Red : XLColor.Black;
            sheet.Cell(currentRow, 11).Value = item.Reason;

            currentRow++;
        }

        currentRow++;
        sheet.Cell(currentRow, 1).Value = "Tổng cộng:";
        sheet.Cell(currentRow, 1).Style.Font.Bold = true;
        sheet.Cell(currentRow, 5).Value = response.Items.Sum(x => x.BookQuantity);
        sheet.Cell(currentRow, 6).Value = response.Items.Sum(x => x.PhysicalQuantity);
        sheet.Cell(currentRow, 7).Value = response.TotalDifferenceValue;
        sheet.Cell(currentRow, 7).Style.Font.Bold = true;
        sheet.Cell(currentRow, 7).Style.Font.FontColor = response.TotalDifferenceValue != 0 ? XLColor.Red : XLColor.Black;
        sheet.Cell(currentRow, 8).Value = response.TotalBookValue;
        sheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
        sheet.Cell(currentRow, 8).Style.Font.Bold = true;
        sheet.Cell(currentRow, 9).Value = response.TotalPhysicalValue;
        sheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
        sheet.Cell(currentRow, 9).Style.Font.Bold = true;
        sheet.Cell(currentRow, 10).Value = response.TotalDifferenceValue;
        sheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
        sheet.Cell(currentRow, 10).Style.Font.Bold = true;
        sheet.Cell(currentRow, 10).Style.Font.FontColor = response.TotalDifferenceValue != 0 ? XLColor.Red : XLColor.Black;

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
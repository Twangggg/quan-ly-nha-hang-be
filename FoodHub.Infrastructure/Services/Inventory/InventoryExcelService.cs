using ClosedXML.Excel;
using FoodHub.Application.Interfaces.Inventory;
using System.IO;

namespace FoodHub.Infrastructure.Services.Inventory;

public class InventoryExcelService : IInventoryExcelService
{
    public Task<List<InventoryBalanceImportDto>> ParseExcelFileAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<InventoryBalanceImportDto>();

        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheet(1);

        var usedRange = sheet.RangeUsed();
        if (usedRange == null)
        {
            return Task.FromResult(result);
        }

        var rows = usedRange.RowsUsed();
        var rowCount = rows.Count();

        if (rowCount < 2)
        {
            return Task.FromResult(result);
        }

        var headerRow = sheet.Row(1);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        }

        var codeCol = headers.TryGetValue("Mã nguyên liệu", out var c1) ? c1 : (headers.TryGetValue("MaNguyenLieu", out var c2) ? c2 : 0);
        var quantityCol = headers.TryGetValue("Số lượng", out var q1) ? q1 : (headers.TryGetValue("SoLuong", out var q2) ? q2 : 0);
        var priceCol = headers.TryGetValue("Giá nhập", out var p1) ? p1 : (headers.TryGetValue("GiaNhap", out var p2) ? p2 : 0);
        var unitCol = headers.TryGetValue("Đơn vị", out var u1) ? u1 : (headers.TryGetValue("DonVi", out var u2) ? u2 : 0);

        if (codeCol == 0 || quantityCol == 0 || priceCol == 0)
        {
            throw new InvalidOperationException("File Excel phải có các cột: Mã nguyên liệu, Số lượng, Giá nhập");
        }

        for (int row = 2; row <= rowCount; row++)
        {
            var codeCell = sheet.Cell(row, codeCol);
            var quantityCell = sheet.Cell(row, quantityCol);
            var priceCell = sheet.Cell(row, priceCol);
            var unitCell = unitCol > 0 ? sheet.Cell(row, unitCol) : null;

            var code = codeCell.GetString()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(code))
            {
                continue;
            }

            var quantity = quantityCell.GetValue<decimal>();
            var price = priceCell.GetValue<decimal>();
            var unit = unitCell?.GetString()?.Trim();

            result.Add(new InventoryBalanceImportDto
            {
                IngredientCode = code,
                Quantity = quantity,
                CostPrice = price,
                Unit = string.IsNullOrEmpty(unit) ? null : unit,
                RowNumber = row
            });
        }

        return Task.FromResult(result);
    }
}
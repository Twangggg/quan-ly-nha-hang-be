namespace FoodHub.Application.Interfaces.Reporting;

public interface IInventoryCheckExcelService
{
    byte[] ExportInventoryCheckToExcel(
        ExportInventoryCheckResponse response
    );
}
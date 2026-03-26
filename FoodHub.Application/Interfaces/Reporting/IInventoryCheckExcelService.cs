using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.ExportInventoryCheck;

namespace FoodHub.Application.Interfaces.Reporting;

public interface IInventoryCheckExcelService
{
    byte[] ExportInventoryCheckToExcel(
        ExportInventoryCheckResponse response
    );
}
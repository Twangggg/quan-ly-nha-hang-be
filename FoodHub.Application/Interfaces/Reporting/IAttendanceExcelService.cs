using FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport;

namespace FoodHub.Application.Interfaces.Reporting
{
    public interface IAttendanceExcelService
    {
        byte[] ExportAttendanceReportToExcel(List<GetAttendanceReportResponse> items);
    }
}

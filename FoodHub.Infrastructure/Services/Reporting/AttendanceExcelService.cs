using ClosedXML.Excel;
using FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport;
using FoodHub.Application.Interfaces.Reporting;
using System.IO;

namespace FoodHub.Infrastructure.Services.Reporting
{
    public class AttendanceExcelService : IAttendanceExcelService
    {
        public byte[] ExportAttendanceReportToExcel(List<GetAttendanceReportResponse> items)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Báo cáo chấm công");

            sheet.Cell(1, 1).Value = "Ngày";
            sheet.Cell(1, 2).Value = "Nhân viên";
            sheet.Cell(1, 3).Value = "Ca làm";
            sheet.Cell(1, 4).Value = "Giờ vào";
            sheet.Cell(1, 5).Value = "Giờ ra";
            sheet.Cell(1, 6).Value = "Trạng thái";

            sheet.Range(1, 1, 1, 6).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                var item = items[i];

                sheet.Cell(row, 1).Value = item.Date.ToString("dd/MM/yyyy");
                sheet.Cell(row, 2).Value = item.EmployeeName;
                sheet.Cell(row, 3).Value = item.ShiftName;
                sheet.Cell(row, 4).Value = item.CheckInTime.ToString("HH:mm");
                
                if (item.CheckOutTime.HasValue)
                {
                    sheet.Cell(row, 5).Value = item.CheckOutTime.Value.ToString("HH:mm");
                }
                
                sheet.Cell(row, 6).Value = item.Status;

                if (item.Status != "Đúng giờ" && item.Status != "Làm ngoài giờ") 
                {
                    sheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
                }
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Reports.Queries.GetDailyReport
{
    public class GetDailyReportQuery : IRequest<Result<GetDailyReportResponse>>
    {
        /// <summary>
        /// Ngày cần xem báo cáo (định dạng yyyy-MM-dd).
        /// Mặc định là hôm nay theo múi giờ Asia/Ho_Chi_Minh.
        /// </summary>
        public DateOnly? Date { get; set; }

        /// <summary>
        /// Số ngày dùng để tính moving average target.
        /// Mặc định 30. Không được nhỏ hơn 1.
        /// </summary>
        public int MovingAverageDays { get; set; } = 30;
    }
}

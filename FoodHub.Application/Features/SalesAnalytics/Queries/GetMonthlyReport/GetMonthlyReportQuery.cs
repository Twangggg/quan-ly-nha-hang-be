using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetMonthlyReport
{
    public class GetMonthlyReportQuery : IRequest<Result<GetMonthlyReportResponse>>
    {
        /// <summary>
        /// Năm cần báo cáo.
        /// Mặc định: Năm hiện tại theo múi giờ VN.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Tháng cần báo cáo (1-12).
        /// Mặc định: Tháng hiện tại theo múi giờ VN.
        /// </summary>
        public int? Month { get; set; }
    }
}

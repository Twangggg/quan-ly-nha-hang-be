using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers
{
    public class GetBestSellersQuery : IRequest<Result<GetBestSellersResponse>>
    {
        /// <summary>
        /// Bộ lọc từ ngày
        /// </summary>
        public DateOnly? StartDate { get; set; }

        /// <summary>
        /// Bộ lọc đến ngày
        /// </summary>
        public DateOnly? EndDate { get; set; }

        /// <summary>
        /// Số lượng bản ghi muốn lấy (Mặc định: 10)
        /// </summary>
        public int Top { get; set; } = 10;
    }
}

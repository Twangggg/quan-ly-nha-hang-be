using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetRevenueChart
{
    public class GetRevenueChartQuery : IRequest<Result<GetRevenueChartResponse>>
    {
        public DateOnly? Date { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
    }
}

using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport
{
    public class GetCategoryReportQuery : IRequest<Result<GetCategoryReportResponse>>
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}

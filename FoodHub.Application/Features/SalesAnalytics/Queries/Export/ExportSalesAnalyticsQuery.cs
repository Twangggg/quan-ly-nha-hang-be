using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.Export
{
    public class ExportSalesAnalyticsQuery : IRequest<Result<byte[]>>
    {
        public DateOnly? Date { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}

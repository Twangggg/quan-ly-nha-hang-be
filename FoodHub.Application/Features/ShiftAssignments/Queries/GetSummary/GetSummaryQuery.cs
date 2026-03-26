using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetSummary
{
    public class GetSummaryQuery : IRequest<Result<GetSummaryResponse>>
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}

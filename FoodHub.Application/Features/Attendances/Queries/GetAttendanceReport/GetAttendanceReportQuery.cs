using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport
{
    public record GetAttendanceReportQuery(
        PaginationParams Pagination,
        DateOnly? Date = null,
        DateOnly? StartDate = null,
        DateOnly? EndDate = null) : IRequest<Result<PagedResult<GetAttendanceReportResponse>>>;
}

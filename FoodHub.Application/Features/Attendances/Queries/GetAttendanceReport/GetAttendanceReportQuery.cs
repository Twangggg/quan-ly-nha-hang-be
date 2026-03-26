using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport
{
    public record GetAttendanceReportQuery(PaginationParams Pagination) : IRequest<Result<PagedResult<GetAttendanceReportResponse>>>;
}

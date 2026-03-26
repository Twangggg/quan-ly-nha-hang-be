using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Attendances.Queries.ExportAttendanceReport
{
    public record ExportAttendanceReportQuery(
        PaginationParams Pagination,
        DateOnly? Date = null,
        DateOnly? StartDate = null,
        DateOnly? EndDate = null) : IRequest<Result<byte[]>>;
}

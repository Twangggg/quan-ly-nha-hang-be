using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Attendances.Queries.ExportAttendanceReport
{
    public record ExportAttendanceReportQuery(PaginationParams Pagination) : IRequest<Result<byte[]>>;
}

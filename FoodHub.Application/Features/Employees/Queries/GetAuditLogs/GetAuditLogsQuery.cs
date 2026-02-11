using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Employees.Queries.GetAuditLogs
{
    public record GetAuditLogsQuery(Guid EmployeeId, PaginationParams Pagination) : IRequest<Result<PagedResult<GetAuditLogsResponse>>>;
}

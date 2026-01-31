using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.Employees.Queries.GetAuditLogs
{
    public record GetAuditLogsQuery(Guid EmployeeId, PaginationParams Pagination) : IRequest<PagedResult<GetAuditLogsResponse>>;
}

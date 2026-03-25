using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Orders.Queries.GetOrderAuditLogs
{
    public record GetAllOrderAuditLogsQuery(PaginationParams Pagination)
        : IRequest<Result<PagedResult<GetOrderAuditLogsResponse>>>;
}

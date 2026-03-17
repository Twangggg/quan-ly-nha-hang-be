using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsQuery : PaginationParams, IRequest<Result<PagedResult<GetAuditLogsResponse>>>
    {
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public string? ActionFilter { get; set; }
        public string? EntityNameFilter { get; set; }
        public string? EntityIdFilter { get; set; }
    }
}

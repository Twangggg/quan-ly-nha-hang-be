using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

public class GetKdsAuditLogsQuery : IRequest<Result<List<GetKdsAuditLogsResponse>>>
{
    public string? Station { get; set; }
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

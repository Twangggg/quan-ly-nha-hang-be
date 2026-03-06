using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

/// <summary>
/// Query parameters for retrieving KDS audit logs
/// </summary>
public class GetKdsAuditLogsQuery : IRequest<Result<PagedResult<GetKdsAuditLogsResponse>>>
{
    /// <summary>
    /// Filter by station name (optional). Use "all" to get all stations.
    /// </summary>
    public string? Station { get; set; }

    /// <summary>
    /// Filter by action type (optional). Use "all" to get all actions.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter logs from this date (optional)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter logs until this date (optional)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Page number (1-based). Default is 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Default is 50.
    /// </summary>
    public int PageSize { get; set; } = 50;
}

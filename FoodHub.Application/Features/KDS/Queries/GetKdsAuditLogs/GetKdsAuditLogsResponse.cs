namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

/// <summary>
/// Response model for a single KDS audit log entry
/// </summary>
public class GetKdsAuditLogsResponse
{
    /// <summary>
    /// Unique identifier of the audit log entry
    /// </summary>
    public Guid LogId { get; set; }

    /// <summary>
    /// Timestamp when the action was performed
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Formatted time string (HH:mm dd/MM)
    /// </summary>
    public string FormattedTime => CreatedAt.ToString("HH:mm dd/MM");

    /// <summary>
    /// Unique identifier of the order
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Order code for display
    /// </summary>
    public string OrderCode { get; set; } = string.Empty;

    /// <summary>
    /// Action type (e.g., KDS_StartCooking, KDS_MarkReady)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable action name
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the employee who performed the action
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Name of the employee who performed the action
    /// </summary>
    public string ActorName { get; set; } = string.Empty;

    /// <summary>
    /// Role of the employee who performed the action
    /// </summary>
    public string ActorRole { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the action (e.g., rejection reason)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// JSON string containing order item details
    /// </summary>
    public string OrderItems { get; set; } = string.Empty;
}

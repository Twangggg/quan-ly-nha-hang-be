namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

public class GetKdsAuditLogsResponse
{
    public Guid LogId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedTime => CreatedAt.ToString("HH:mm dd/MM");
    
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    
    public string Action { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    
    public Guid EmployeeId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    
    public string? Reason { get; set; }
    public string OrderItems { get; set; } = string.Empty;
}

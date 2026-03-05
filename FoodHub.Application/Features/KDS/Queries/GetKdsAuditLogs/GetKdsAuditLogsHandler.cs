using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

public class GetKdsAuditLogsHandler : IRequestHandler<GetKdsAuditLogsQuery, Result<List<GetKdsAuditLogsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKdsAuditLogsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<GetKdsAuditLogsResponse>>> Handle(
        GetKdsAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var kdsActions = new[]
        {
            AuditLogActions.KdsStartCooking,
            AuditLogActions.KdsMarkReady,
            AuditLogActions.KdsReject,
            AuditLogActions.KdsReturn
        };

        var query = _unitOfWork
            .Repository<OrderAuditLog>()
            .Query()
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.Employee)
            .Where(x => kdsActions.Contains(x.Action));

        // Filter by station (from Order's station or OrderItem's station)
        if (!string.IsNullOrEmpty(request.Station) && request.Station != "all")
        {
            query = query.Where(x => x.Order.OrderItems.Any(oi => oi.StationSnapshot == request.Station));
        }

        // Filter by action
        if (!string.IsNullOrEmpty(request.Action) && request.Action != "all")
        {
            query = query.Where(x => x.Action == request.Action);
        }

        // Filter by date
        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= request.ToDate.Value);
        }

        var logs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var response = logs.Select(log => new GetKdsAuditLogsResponse
        {
            LogId = log.LogId,
            CreatedAt = log.CreatedAt,
            OrderId = log.OrderId,
            OrderCode = log.Order?.OrderCode ?? string.Empty,
            Action = log.Action,
            ActionName = GetActionName(log.Action),
            EmployeeId = log.EmployeeId,
            ActorName = log.Employee?.FullName ?? "Unknown",
            ActorRole = log.Employee.Role.ToString(),
            Reason = log.ChangeReason,
            OrderItems = log.NewValue ?? string.Empty
        }).ToList();

        return Result<List<GetKdsAuditLogsResponse>>.Success(response);
    }

    private static string GetActionName(string action)
    {
        return action switch
        {
            AuditLogActions.KdsStartCooking => "Bắt đầu nấu",
            AuditLogActions.KdsMarkReady => "Hoàn thành",
            AuditLogActions.KdsReject => "Từ chối",
            AuditLogActions.KdsReturn => "Trả lại",
            _ => action
        };
    }
}

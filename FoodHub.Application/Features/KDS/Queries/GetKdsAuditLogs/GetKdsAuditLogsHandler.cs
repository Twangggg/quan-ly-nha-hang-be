using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

public class GetKdsAuditLogsHandler
    : IRequestHandler<GetKdsAuditLogsQuery, Result<PagedResult<GetKdsAuditLogsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetKdsAuditLogsHandler> _logger;

    public GetKdsAuditLogsHandler(IUnitOfWork unitOfWork, ILogger<GetKdsAuditLogsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PagedResult<GetKdsAuditLogsResponse>>> Handle(
        GetKdsAuditLogsQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "GetKdsAuditLogs started. Station: {Station}, Action: {Action}, Page: {PageNumber}, PageSize: {PageSize}",
            request.Station,
            request.Action,
            request.PageNumber,
            request.PageSize
        );

        var kdsActions = new[]
        {
            AuditLogActions.KdsStartCooking,
            AuditLogActions.KdsMarkReady,
            AuditLogActions.KdsReject,
            AuditLogActions.KdsReturn,
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
            query = query.Where(x =>
                x.Order.OrderItems.Any(oi => oi.StationSnapshot == request.Station)
            );
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

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedLogs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(log => new
            {
                log.LogId,
                log.CreatedAt,
                log.OrderId,
                OrderCode = log.Order != null ? log.Order.OrderCode : string.Empty,
                log.Action,
                log.EmployeeId,
                ActorName = log.Employee != null ? log.Employee.FullName : "Unknown",
                ActorRole = log.Employee != null
                    ? (FoodHub.Domain.Enums.EmployeeRole?)log.Employee.Role
                    : null,
                Reason = log.ChangeReason,
                OrderItems = log.NewValue, // Project raw value here
            })
            .ToListAsync(cancellationToken);

        var responseData = pagedLogs
            .Select(log => new GetKdsAuditLogsResponse
            {
                LogId = log.LogId,
                CreatedAt = log.CreatedAt,
                OrderId = log.OrderId,
                OrderCode = log.OrderCode,
                Action = log.Action,
                ActionName = GetActionName(log.Action),
                EmployeeId = log.EmployeeId,
                ActorName = log.ActorName,
                ActorRole = log.ActorRole?.ToString() ?? "Unknown",
                Reason = log.Reason,
                OrderItems = log.OrderItems ?? string.Empty, // Handle null-coalescing here
            })
            .ToList();

        var paginationParams = new PaginationParams
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };

        var result = new PagedResult<GetKdsAuditLogsResponse>(
            responseData,
            paginationParams,
            totalCount
        );

        _logger.LogInformation(
            "GetKdsAuditLogs completed. Total: {TotalCount}, Returned: {ReturnedCount}, Page: {PageNumber}/{TotalPages}",
            totalCount,
            responseData.Count,
            request.PageNumber,
            (int)Math.Ceiling((double)totalCount / request.PageSize)
        );

        return Result<PagedResult<GetKdsAuditLogsResponse>>.Success(result);
    }

    private static string GetActionName(string action)
    {
        return action switch
        {
            AuditLogActions.KdsStartCooking => "Bắt đầu nấu",
            AuditLogActions.KdsMarkReady => "Hoàn thành",
            AuditLogActions.KdsReject => "Từ chối",
            AuditLogActions.KdsReturn => "Trả lại",
            _ => action,
        };
    }
}

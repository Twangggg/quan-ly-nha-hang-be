using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Orders.Queries.GetOrderAuditLogs
{
    public class GetAllOrderAuditLogsHandler
        : IRequestHandler<GetAllOrderAuditLogsQuery, Result<PagedResult<GetOrderAuditLogsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllOrderAuditLogsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<GetOrderAuditLogsResponse>>> Handle(
            GetAllOrderAuditLogsQuery request,
            CancellationToken cancellationToken
        )
        {
            var query = _unitOfWork
                .Repository<OrderAuditLog>()
                .Query()
                .AsNoTracking()
                .Where(log => log.OrderId != Guid.Empty)
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new GetOrderAuditLogsResponse
                {
                    LogId = log.LogId,
                    CreatedAt = log.CreatedAt,
                    OrderId = log.OrderId,
                    OrderCode = log.Order != null ? log.Order.OrderCode : string.Empty,
                    Action = log.Action,
                    ActionName = GetActionName(log.Action),
                    EmployeeId = log.EmployeeId,
                    ActorName = log.Employee != null ? log.Employee.FullName : "Unknown",
                    ActorRole = log.Employee != null ? log.Employee.Role.ToString() : "Unknown",
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    ChangeReason = log.ChangeReason,
                });

            var search = request.Pagination.Search?.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(log =>
                    log.OrderCode.ToLower().Contains(search)
                    || log.Action.ToLower().Contains(search)
                    || log.ActionName.ToLower().Contains(search)
                    || log.ActorName.ToLower().Contains(search)
                    || log.ActorRole.ToLower().Contains(search)
                    || (log.ChangeReason != null && log.ChangeReason.ToLower().Contains(search))
                );
            }

            var actionFilter = request.Pagination.Filters?
                .Select(filter => filter.Split(':', 2))
                .FirstOrDefault(parts =>
                    parts.Length == 2
                    && parts[0].Trim().Equals("action", StringComparison.OrdinalIgnoreCase)
                )?[1]
                ?.Trim();

            if (!string.IsNullOrWhiteSpace(actionFilter) && !actionFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(log => log.Action == actionFilter);
            }

            var pagedResult = await query.ToPagedResultAsync(request.Pagination, cancellationToken);
            return Result<PagedResult<GetOrderAuditLogsResponse>>.Success(pagedResult);
        }

        private static string GetActionName(string action)
        {
            return action switch
            {
                AuditLogActions.CreateOrder => "Tao don",
                AuditLogActions.SubmitOrder => "Gui bep",
                AuditLogActions.AddOrderItem => "Them mon",
                AuditLogActions.UpdateOrderItem => "Cap nhat mon",
                AuditLogActions.CancelOrderItem => "Huy mon",
                AuditLogActions.CancelOrder => "Huy don",
                AuditLogActions.CompleteOrder => "Hoan tat don",
                AuditLogActions.MergeOrder => "Gop don",
                AuditLogActions.SplitOrder => "Tach don",
                OrderAuditActions.SplitBill => "Tach bill",
                AuditLogActions.ChangeOrderTable => "Chuyen ban",
                AuditLogActions.CheckoutOrder => "Thanh toan",
                AuditLogActions.KdsStartCooking => "Bat dau nau",
                AuditLogActions.KdsCompleteCooking => "Hoan thanh nau",
                AuditLogActions.KdsReject => "Tu choi mon",
                AuditLogActions.KdsReturn => "Tra mon ve hang doi",
                AuditLogActions.CheckInReservation => "Check-in dat ban",
                AuditLogActions.AdjustOrderItemQuantity => "Dieu chinh so luong",
                "ApplyPromotion" => "Ap dung khuyen mai",
                _ => action,
            };
        }
    }
}

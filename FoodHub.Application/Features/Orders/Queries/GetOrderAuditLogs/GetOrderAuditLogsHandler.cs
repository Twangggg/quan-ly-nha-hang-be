using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Orders.Queries.GetOrderAuditLogs
{
    public class GetOrderAuditLogsHandler
        : IRequestHandler<GetOrderAuditLogsQuery, Result<PagedResult<GetOrderAuditLogsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public GetOrderAuditLogsHandler(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<PagedResult<GetOrderAuditLogsResponse>>> Handle(
            GetOrderAuditLogsQuery request,
            CancellationToken cancellationToken
        )
        {
            var orderExists = await _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .AnyAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (!orderExists)
            {
                return Result<PagedResult<GetOrderAuditLogsResponse>>.NotFound(
                    _messageService.GetMessage(MessageKeys.Order.NotFound)
                );
            }

            var query = _unitOfWork
                .Repository<OrderAuditLog>()
                .Query()
                .AsNoTracking()
                .Where(log => log.OrderId == request.OrderId)
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

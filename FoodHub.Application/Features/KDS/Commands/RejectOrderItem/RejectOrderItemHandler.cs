using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Commands.RejectOrderItem
{
    public class RejectOrderItemHandler : IRequestHandler<RejectOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly ILogger<RejectOrderItemHandler> _logger;

        public RejectOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            KdsPriorityCalculator priorityCalculator,
            ILogger<RejectOrderItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _priorityCalculator = priorityCalculator;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            RejectOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Attempting to reject OrderItemId: {OrderItemId}. Reason: {Reason}",
                request.OrderItemId,
                request.Reason
            );

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();
            var orderItem = await orderItemRepository
                .Query()
                .FirstOrDefaultAsync(
                    oi => oi.OrderItemId == request.OrderItemId,
                    cancellationToken
                );

            if (orderItem == null)
            {
                _logger.LogWarning("OrderItem not found: {OrderItemId}", request.OrderItemId);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.NotFound),
                    ResultErrorType.NotFound
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var domainResult = orderItem.Reject(request.Reason);
                if (!domainResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Domain logic failed for Reject: {OrderItemId}. Error: {Error}",
                        request.OrderItemId,
                        domainResult.ErrorCode
                    );
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(domainResult.ErrorCode!)
                    );
                }

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = orderItem.OrderId,
                    EmployeeId = Guid.Parse(_currentUserService.UserId!),
                    Action = AuditLogActions.KdsReject,
                    OldValue = orderItem.Status.ToString(),
                    NewValue = OrderItemStatus.Rejected.ToString(),
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                // Auto-pull: Tìm món tiếp theo dựa trên độ ưu tiên thông minh
                var pendingItems = await orderItemRepository
                    .Query()
                    .Include(oi => oi.Order)
                    .Where(oi =>
                        oi.StationSnapshot == orderItem.StationSnapshot
                        && oi.Status == OrderItemStatus.Preparing
                    )
                    .ToListAsync(cancellationToken);

                OrderItem? nextItem = null;
                if (pendingItems.Any())
                {
                    nextItem = pendingItems
                        .OrderByDescending(oi => _priorityCalculator.Calculate(oi, oi.Order))
                        .ThenBy(oi => oi.CreatedAt)
                        .First();
                }

                if (nextItem != null)
                {
                    _logger.LogInformation(
                        "Auto-pulling next item after rejection: {NextItemId} for Station: {Station}",
                        nextItem.OrderItemId,
                        orderItem.StationSnapshot
                    );
                    nextItem.StartCooking();

                    var autoPullLog = new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = nextItem.OrderId,
                        EmployeeId = Guid.Parse(_currentUserService.UserId!),
                        Action = AuditLogActions.KdsStartCooking,
                        OldValue = OrderItemStatus.Preparing.ToString(),
                        NewValue = OrderItemStatus.Cooking.ToString(),
                        ChangeReason = "Auto-pull (Station Slot Freed by Rejection)",
                        CreatedAt = DateTime.UtcNow,
                    };
                    await _unitOfWork.Repository<OrderAuditLog>().AddAsync(autoPullLog);
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully rejected OrderItemId: {OrderItemId}",
                    request.OrderItemId
                );

                // SignalR Notify
                _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                    orderItem.OrderItemId,
                    OrderItemStatus.Rejected,
                    orderItem.StationSnapshot
                );
                if (nextItem != null)
                {
                    _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                        nextItem.OrderItemId,
                        OrderItemStatus.Cooking,
                        nextItem.StationSnapshot
                    );
                }

                return Result<Guid>.Success(orderItem.OrderItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while rejecting OrderItemId: {OrderItemId}",
                    request.OrderItemId
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem
{
    public class CancelOrderItemHandler : IRequestHandler<CancelOrderItemCommand, Result<CancelOrderItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISignalRService _signalRService;
        private readonly IMapper _mapper;
        private readonly ILogger<CancelOrderItemHandler> _logger;

        public CancelOrderItemHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ISignalRService signalRService,
            IMapper mapper,
            ILogger<CancelOrderItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _signalRService = signalRService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CancelOrderItemResponse>> Handle(
            CancelOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                _logger.LogWarning(
                    "Unauthorized cancel attempt for OrderItemId {OrderItemId}",
                    request.OrderItemId
                );
                return Result<CancelOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                "Canceling OrderItem {OrderItemId}. Reason: {Reason}, RequestedBy: {UserId}",
                request.OrderItemId,
                request.Reason,
                auditorId.Value
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
                _logger.LogWarning("OrderItem {OrderItemId} not found.", request.OrderItemId);
                return Result<CancelOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.OrderItem.NotFound),
                    ResultErrorType.NotFound
                );
            }

            var domainResult = orderItem.Cancel();
            if (!domainResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Domain validation failed for canceling OrderItem {OrderItemId}. Error: {Error}",
                    request.OrderItemId,
                    domainResult.ErrorCode
                );
                return Result<CancelOrderItemResponse>.Failure(
                    _messageService.GetMessage(
                        domainResult.ErrorCode ?? MessageKeys.Order.InvalidActionWithStatus
                    ),
                    ResultErrorType.BadRequest
                );
            }

            // Using transaction for multi-write (orderItem cancel, order total update, audit log)
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Determine if this item is a combo parent and get all order items
                var allOrderItems = await orderItemRepository
                    .Query()
                    .Where(oi => oi.OrderId == orderItem.OrderId)
                    .ToListAsync(cancellationToken);

                // Log all items with their ComboParentOrderItemId for debugging
                _logger.LogInformation(
                    "CancelOrderItem: Order {OrderId} has {ItemCount} items. All items:\n{AllItems}",
                    orderItem.OrderId,
                    allOrderItems.Count,
                    string.Join("\n", allOrderItems.Select(oi => 
                        $"  - {oi.ItemNameSnapshot}: Status={oi.Status}, ComboParent={oi.ComboParentOrderItemId}"))
                );

                var isComboParent = allOrderItems.Any(oi => oi.ComboParentOrderItemId == orderItem.OrderItemId);
                
                _logger.LogInformation(
                    "CancelOrderItem: Current item {ItemName} (ID: {ItemId}), IsComboParent={IsComboParent}",
                    orderItem.ItemNameSnapshot,
                    orderItem.OrderItemId,
                    isComboParent
                );

                var comboChildrenList = new List<OrderItem>();

                // Clear combo parent reference for this item if it's a child
                if (orderItem.ComboParentOrderItemId.HasValue)
                {
                    _logger.LogInformation(
                        "CancelOrderItem: Item has ComboParentOrderItemId={ParentId}, clearing it",
                        orderItem.ComboParentOrderItemId);
                    orderItem.ComboParentOrderItemId = null;
                    orderItemRepository.Update(orderItem);
                }

                // If this is a combo parent, cancel all children (regardless of their status)
                if (isComboParent)
                {
                    var comboChildren = allOrderItems
                        .Where(oi => oi.ComboParentOrderItemId == orderItem.OrderItemId)
                        .ToList();

                    comboChildrenList.AddRange(comboChildren);

                    _logger.LogInformation(
                        "CancelOrderItem: Found {Count} combo children. Status breakdown: {StatusBreakdown}",
                        comboChildren.Count,
                        string.Join(", ", comboChildren.GroupBy(c => c.Status).Select(g => $"{g.Key}:{g.Count()}"))
                    );

                    foreach (var child in comboChildren)
                    {
                        _logger.LogInformation(
                            "CancelOrderItem: Processing child {ChildName} (Id: {ChildId}, Status: {Status}, ComboParent: {ParentId})",
                            child.ItemNameSnapshot,
                            child.OrderItemId,
                            child.Status,
                            child.ComboParentOrderItemId
                        );

                        // Skip if already cancelled
                        if (child.Status == OrderItemStatus.Cancelled)
                        {
                            _logger.LogInformation("CancelOrderItem: Child {ChildId} already cancelled, skipping", child.OrderItemId);
                            continue;
                        }

                        child.Status = OrderItemStatus.Cancelled;
                        child.CancelledAt = DateTime.UtcNow;
                        child.UpdatedAt = DateTime.UtcNow;
                        child.ComboParentOrderItemId = null; // Remove parent reference
                        orderItemRepository.Update(child);

                        var childAuditLog = new OrderAuditLog
                        {
                            LogId = Guid.NewGuid(),
                            OrderId = child.OrderId,
                            EmployeeId = auditorId.Value,
                            Action = AuditLogActions.CancelOrderItem,
                            CreatedAt = DateTime.UtcNow,
                            ChangeReason = request.Reason,
                            NewValue =
                                $"{{\"orderItemId\": \"{child.OrderItemId}\", \"status\": \"Cancelled\", \"parentId\": \"{orderItem.OrderItemId}\"}}",
                        };
                        await _unitOfWork.Repository<OrderAuditLog>().AddAsync(childAuditLog);
                    }
                }

                orderItemRepository.Update(orderItem);

                var order = await _unitOfWork
                    .Repository<Domain.Entities.Order>()
                    .Query()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.OptionGroups)
                            .ThenInclude(og => og.OptionValues)
                    .Include(o => o.Promotion)
                    .FirstOrDefaultAsync(o => o.OrderId == orderItem.OrderId, cancellationToken);

                if (order != null)
                {
                    order.RecalculateTotalAmount();
                    order.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<Domain.Entities.Order>().Update(order);
                }

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = orderItem.OrderId,
                    EmployeeId = auditorId.Value,
                    Action = AuditLogActions.CancelOrderItem,
                    CreatedAt = DateTime.UtcNow,
                    ChangeReason = request.Reason,
                    NewValue =
                        $"{{\"orderItemId\": \"{orderItem.OrderItemId}\", \"status\": \"Cancelled\", \"childrenCancelled\": {comboChildrenList.Count}}}",
                };

                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully canceled OrderItem {OrderItemId} for Order {OrderId}",
                    request.OrderItemId,
                    orderItem.OrderId
                );

                // Notify KDS via SignalR for the cancelled parent
                await _signalRService.NotifyOrderItemStatusChangedAsync(
                    orderItem.OrderItemId,
                    OrderItemStatus.Cancelled,
                    orderItem.StationSnapshot
                );

                // Notify KDS for all cancelled combo children
                foreach (var child in comboChildrenList)
                {
                    await _signalRService.NotifyOrderItemStatusChangedAsync(
                        child.OrderItemId,
                        OrderItemStatus.Cancelled,
                        child.StationSnapshot
                    );
                }

                var response = _mapper.Map<CancelOrderItemResponse>(order);
                return Result<CancelOrderItemResponse>.Success(response);
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database error while canceling OrderItem {OrderItemId}",
                    request.OrderItemId
                );
                return Result<CancelOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.Conflict
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Unexpected error while canceling OrderItem {OrderItemId}",
                    request.OrderItemId
                );
                throw;
            }
        }
    }
}

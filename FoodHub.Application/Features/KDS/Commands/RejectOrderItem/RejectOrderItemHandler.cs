using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
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
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly IKdsAutoPullService _kdsAutoPullService;
        private readonly ILogger<RejectOrderItemHandler> _logger;

        public RejectOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            KdsPriorityCalculator priorityCalculator,
            IKdsSettingsProvider kdsSettingsProvider,
            IKdsAutoPullService kdsAutoPullService,
            ILogger<RejectOrderItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _priorityCalculator = priorityCalculator;
            _kdsSettingsProvider = kdsSettingsProvider;
            _kdsAutoPullService = kdsAutoPullService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            RejectOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                _logger.LogWarning(
                    "Unauthorized reject attempt for OrderItemId {OrderItemId}",
                    request.OrderItemId
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

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
                var itemsToReject = new List<OrderItem> { orderItem };

                // If this item is a combo child, reject the entire combo (parent + all children)
                if (orderItem.ComboParentOrderItemId != null)
                {
                    var comboItems = await orderItemRepository
                        .Query()
                        .Where(oi =>
                            oi.ComboParentOrderItemId == orderItem.ComboParentOrderItemId
                            && (
                                oi.Status == OrderItemStatus.Preparing
                                || oi.Status == OrderItemStatus.Cooking
                            )
                        )
                        .ToListAsync(cancellationToken);

                    var parentItem = await orderItemRepository
                        .Query()
                        .FirstOrDefaultAsync(oi =>
                            oi.OrderItemId == orderItem.ComboParentOrderItemId
                            && (
                                oi.Status == OrderItemStatus.Preparing
                                || oi.Status == OrderItemStatus.Cooking
                            ),
                            cancellationToken
                        );

                    if (parentItem != null)
                    {
                        itemsToReject.Add(parentItem);
                    }

                    itemsToReject.AddRange(comboItems);
                }

                var rejectedItemIds = new List<Guid>();

                foreach (var itemToReject in itemsToReject.DistinctBy(i => i.OrderItemId))
                {
                    var oldStatus = itemToReject.Status;
                    var domainResult = itemToReject.Reject(request.Reason);
                    if (!domainResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Domain logic failed for Reject: {OrderItemId}. Error: {Error}",
                            itemToReject.OrderItemId,
                            domainResult.ErrorCode
                        );
                        continue;
                    }

                    var auditLog = new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = itemToReject.OrderId,
                        EmployeeId = auditorId.Value,
                        Action = AuditLogActions.KdsReject,
                        OldValue = $"\"{oldStatus}\"",
                        NewValue = $"\"{OrderItemStatus.Rejected}\"",
                        CreatedAt = DateTime.UtcNow,
                    };
                    await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                    orderItemRepository.Update(itemToReject);
                    rejectedItemIds.Add(itemToReject.OrderItemId);
                }

                if (!rejectedItemIds.Any())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.OrderItem.MustBeCookingToReject)
                    );
                }

                var order = await _unitOfWork
                    .Repository<Order>()
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
                    _unitOfWork.Repository<Order>().Update(order);
                }

                // Save first to free up slot
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Auto-pull next item if capacity allows
                var pulledItems = await _kdsAutoPullService.ProcessAutoPullAsync(
                    orderItem.StationSnapshot,
                    auditorId.Value,
                    cancellationToken
                );

                // Save pulled items
                if (pulledItems.Any())
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully rejected OrderItemIds: [{ItemIds}]",
                    string.Join(", ", rejectedItemIds)
                );

                // Notify for all rejected items (including combo parent/children)
                var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
                foreach (var rejectedId in rejectedItemIds)
                {
                    var rejectedOrderItem = itemsToReject.First(i => i.OrderItemId == rejectedId);

                    _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                        rejectedOrderItem.OrderItemId,
                        OrderItemStatus.Rejected,
                        rejectedOrderItem.StationSnapshot
                    );

                    var response = KdsMappingHelper.MapToResponse(
                        rejectedOrderItem,
                        _priorityCalculator,
                        settings
                    );
                    _ = _signalRService.NotifyKdsItemUpdatedAsync(
                        rejectedOrderItem.StationSnapshot,
                        response
                    );
                }

                // Notify for Pulled Items
                if (pulledItems.Any())
                {
                    foreach (var pulledItem in pulledItems)
                    {
                        var response = KdsMappingHelper.MapToResponse(
                            pulledItem,
                            _priorityCalculator,
                            settings
                        );
                        _ = _signalRService.NotifyKdsItemUpdatedAsync(
                            pulledItem.StationSnapshot,
                            response
                        );
                        _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                            pulledItem.OrderItemId,
                            OrderItemStatus.Cooking,
                            pulledItem.StationSnapshot
                        );
                    }
                }

                return Result<Guid>.Success(orderItem.OrderItemId);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

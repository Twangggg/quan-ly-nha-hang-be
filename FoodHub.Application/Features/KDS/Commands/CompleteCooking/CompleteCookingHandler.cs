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

namespace FoodHub.Application.Features.KDS.Commands.CompleteCooking
{
    public class CompleteCookingHandler : IRequestHandler<CompleteCookingCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly IKdsAutoPullService _kdsAutoPullService;
        private readonly IInventoryDeductionService _inventoryDeductionService;
        private readonly ILogger<CompleteCookingHandler> _logger;

        public CompleteCookingHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            KdsPriorityCalculator priorityCalculator,
            IKdsSettingsProvider kdsSettingsProvider,
            IKdsAutoPullService kdsAutoPullService,
            IInventoryDeductionService inventoryDeductionService,
            ILogger<CompleteCookingHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _priorityCalculator = priorityCalculator;
            _kdsSettingsProvider = kdsSettingsProvider;
            _kdsAutoPullService = kdsAutoPullService;
            _inventoryDeductionService = inventoryDeductionService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            CompleteCookingCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                _logger.LogWarning(
                    "Unauthorized complete cooking attempt for OrderItemId {OrderItemId}",
                    request.OrderItemId
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                "Attempting to complete cooking for OrderItemId: {OrderItemId}",
                request.OrderItemId
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
                    _messageService.GetMessage(MessageKeys.OrderItem.NotFound),
                    ResultErrorType.NotFound
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var oldStatus = orderItem.Status;
                var domainResult = orderItem.CompleteCooking();
                if (!domainResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Domain logic failed for CompleteCooking: {OrderItemId}. Error: {Error}",
                        request.OrderItemId,
                        domainResult.ErrorCode
                    );
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.OrderItem.MustBeCookingToComplete)
                    );
                }

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = orderItem.OrderId,
                    EmployeeId = auditorId.Value,
                    Action = AuditLogActions.KdsCompleteCooking,
                    OldValue = $"\"{oldStatus}\"",
                    NewValue = $"\"{OrderItemStatus.Completed}\"",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                orderItemRepository.Update(orderItem);

                // Auto-set combo parent to Cooking when any child starts cooking
                await UpdateComboParentToCookingIfNeededAsync(
                    orderItem,
                    orderItemRepository,
                    auditorId.Value,
                    cancellationToken
                );

                // Auto-complete combo parent if all children are completed
                await AutoCompleteComboParentIfNeededAsync(
                    orderItem,
                    orderItemRepository,
                    auditorId.Value,
                    cancellationToken
                );

                // Save complete first to free up the slot properly
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Auto-pull next item if capacity allows
                var pulledItems = await _kdsAutoPullService.ProcessAutoPullAsync(
                    orderItem.StationSnapshot,
                    auditorId.Value,
                    cancellationToken
                );

                // Save pulled items if any
                if (pulledItems.Any())
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync();

                await _inventoryDeductionService.DeductStockForItemAsync(
                    orderItem.OrderItemId,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Successfully completed cooking and handled auto-pull for OrderItemId: {OrderItemId}",
                    request.OrderItemId
                );

                // Notify for Completed Item
                _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                    orderItem.OrderItemId,
                    OrderItemStatus.Completed,
                    orderItem.StationSnapshot
                );

                // Notify for Pulled Items
                if (pulledItems.Any())
                {
                    var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
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

        private async Task AutoCompleteComboParentIfNeededAsync(
            OrderItem completedChild,
            IGenericRepository<OrderItem> orderItemRepository,
            Guid auditorId,
            CancellationToken cancellationToken
        )
        {
            if (!completedChild.ComboParentOrderItemId.HasValue)
            {
                return;
            }

            var allOrderItems = await orderItemRepository
                .Query()
                .Where(oi => oi.OrderId == completedChild.OrderId)
                .ToListAsync(cancellationToken);

            var comboParent = allOrderItems.FirstOrDefault(
                oi => oi.OrderItemId == completedChild.ComboParentOrderItemId.Value
            );

            if (comboParent == null)
            {
                return;
            }

            var comboChildren = allOrderItems
                .Where(oi => oi.ComboParentOrderItemId == comboParent.OrderItemId)
                .ToList();

            var allChildrenCompletedOrCancelled = comboChildren.All(
                child => child.Status == OrderItemStatus.Completed ||
                         child.Status == OrderItemStatus.Cancelled
            );

            if (!allChildrenCompletedOrCancelled)
            {
                return;
            }

            if (comboParent.Status != OrderItemStatus.Preparing &&
                comboParent.Status != OrderItemStatus.Cooking)
            {
                return;
            }

            var oldStatus = comboParent.Status;
            comboParent.Status = OrderItemStatus.Completed;
            comboParent.UpdatedAt = DateTime.UtcNow;
            orderItemRepository.Update(comboParent);

            var auditLog = Domain.Entities.OrderAuditLog.Create(
                comboParent.OrderId,
                auditorId,
                AuditLogActions.KdsCompleteCooking,
                null,
                new { status = OrderItemStatus.Completed.ToString(), note = "Auto-completed when all children finished" }
            );
            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

            _logger.LogInformation(
                "Auto-completed combo parent {ParentId} because all {ChildCount} children are completed/cancelled",
                comboParent.OrderItemId,
                comboChildren.Count
            );

            // Notify KDS about the auto-completed parent
            _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                comboParent.OrderItemId,
                OrderItemStatus.Completed,
                comboParent.StationSnapshot
            );
        }

        private async Task UpdateComboParentToCookingIfNeededAsync(
            OrderItem completedChild,
            IGenericRepository<OrderItem> orderItemRepository,
            Guid auditorId,
            CancellationToken cancellationToken
        )
        {
            if (!completedChild.ComboParentOrderItemId.HasValue)
            {
                return;
            }

            var allOrderItems = await orderItemRepository
                .Query()
                .Where(oi => oi.OrderId == completedChild.OrderId)
                .ToListAsync(cancellationToken);

            var comboParent = allOrderItems.FirstOrDefault(
                oi => oi.OrderItemId == completedChild.ComboParentOrderItemId.Value
            );

            if (comboParent == null)
            {
                return;
            }

            if (comboParent.Status != OrderItemStatus.Preparing)
            {
                return;
            }

            var comboChildren = allOrderItems
                .Where(oi => oi.ComboParentOrderItemId == comboParent.OrderItemId)
                .ToList();

            var anyChildCookingOrCompleted = comboChildren.Any(
                child => child.Status == OrderItemStatus.Cooking ||
                         child.Status == OrderItemStatus.Completed
            );

            if (!anyChildCookingOrCompleted)
            {
                return;
            }

            var oldStatus = comboParent.Status;
            comboParent.Status = OrderItemStatus.Cooking;
            comboParent.UpdatedAt = DateTime.UtcNow;
            orderItemRepository.Update(comboParent);

            var auditLog = OrderAuditLog.Create(
                comboParent.OrderId,
                auditorId,
                AuditLogActions.KdsStartCooking,
                new { status = oldStatus.ToString() },
                new { status = OrderItemStatus.Cooking.ToString(), note = "Auto-set when child started cooking" }
            );
            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

            _logger.LogInformation(
                "Auto-set combo parent {ParentId} to Cooking because child {ChildId} started cooking",
                comboParent.OrderItemId,
                completedChild.OrderItemId
            );

            _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                comboParent.OrderItemId,
                OrderItemStatus.Cooking,
                comboParent.StationSnapshot
            );
        }
    }
}

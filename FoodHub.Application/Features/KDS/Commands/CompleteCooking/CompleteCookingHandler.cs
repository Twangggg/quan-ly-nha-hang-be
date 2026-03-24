using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
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
        private readonly IInventoryDeductionService _inventoryDeductionService;
        private readonly ILogger<CompleteCookingHandler> _logger;

        public CompleteCookingHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            KdsPriorityCalculator priorityCalculator,
            IInventoryDeductionService inventoryDeductionService,
            ILogger<CompleteCookingHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _priorityCalculator = priorityCalculator;
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

                var targetStations = new List<string> { orderItem.StationSnapshot };
                if (
                    orderItem.StationSnapshot == Station.HotKitchen.ToString()
                    || orderItem.StationSnapshot == Station.ColdKitchen.ToString()
                )
                {
                    targetStations =
                    [
                        Station.HotKitchen.ToString(),
                        Station.ColdKitchen.ToString(),
                    ];
                }

                var pendingItems = await orderItemRepository
                    .Query()
                    .Include(oi => oi.Order)
                        .ThenInclude(o => o.OrderItems)
                    .Include(oi => oi.MenuItem)
                    .Where(oi =>
                        targetStations.Contains(oi.StationSnapshot)
                        && oi.Status == OrderItemStatus.Preparing
                    )
                    .ToListAsync(cancellationToken);

                OrderItem? nextItem = null;
                if (pendingItems.Any())
                {
                    nextItem = pendingItems
                        .OrderByDescending(oi =>
                            _priorityCalculator.Calculate(
                                oi.CreatedAt,
                                oi.Order?.IsPriority ?? false,
                                (oi.MenuItem?.ExpectedTime ?? 0) * 60,
                                oi.Order?.OrderType ?? OrderType.DineIn,
                                oi.Order?.OrderItems?.Count ?? 0,
                                oi.Order?.OrderItems?.Count(x =>
                                    x.Status == OrderItemStatus.Completed
                                ) ?? 0
                            )
                        )
                        .ThenBy(oi => oi.CreatedAt)
                        .First();
                }

                if (nextItem != null)
                {
                    _logger.LogInformation(
                        "Auto-pulling next item: {NextItemId} for Station: {Station}",
                        nextItem.OrderItemId,
                        orderItem.StationSnapshot
                    );
                    nextItem.StartCooking();

                    var autoPullLog = new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = nextItem.OrderId,
                        EmployeeId = auditorId.Value,
                        Action = AuditLogActions.KdsStartCooking,
                        OldValue = $"\"{OrderItemStatus.Preparing}\"",
                        NewValue = $"\"{OrderItemStatus.Cooking}\"",
                        ChangeReason = "Auto-pull (Station Slot Freed)",
                        CreatedAt = DateTime.UtcNow,
                    };
                    await _unitOfWork.Repository<OrderAuditLog>().AddAsync(autoPullLog);
                    orderItemRepository.Update(nextItem);
                }

                orderItemRepository.Update(orderItem);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                await _inventoryDeductionService.DeductStockForItemAsync(
                    orderItem.OrderItemId,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Successfully completed cooking and handled auto-pull for OrderItemId: {OrderItemId}",
                    request.OrderItemId
                );

                _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                    orderItem.OrderItemId,
                    OrderItemStatus.Completed,
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
                    "Error occurred while completing cooking for OrderItemId: {OrderItemId}",
                    request.OrderItemId
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

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
                
                // Save complete first to free up the slot properly
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Auto-pull next item if capacity allows
                await _kdsAutoPullService.ProcessAutoPullAsync(orderItem.StationSnapshot, auditorId.Value, cancellationToken);
                
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

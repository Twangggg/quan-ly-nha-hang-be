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

namespace FoodHub.Application.Features.KDS.Commands.ReturnOrderItem
{
    public class ReturnOrderItemHandler : IRequestHandler<ReturnOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly ILogger<ReturnOrderItemHandler> _logger;

        public ReturnOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            KdsPriorityCalculator priorityCalculator,
            IKdsSettingsProvider kdsSettingsProvider,
            ILogger<ReturnOrderItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _priorityCalculator = priorityCalculator;
            _kdsSettingsProvider = kdsSettingsProvider;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            ReturnOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                _logger.LogWarning(
                    "Unauthorized return attempt for OrderItemId {OrderItemId}",
                    request.OrderItemId
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }
            _logger.LogInformation(
                "Attempting to return OrderItemId to queue: {OrderItemId}",
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

            var oldStatus = orderItem.Status;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var domainResult = orderItem.ReturnToQueue();
                if (!domainResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Domain logic failed for ReturnToQueue: {OrderItemId}. Error: {Error}",
                        request.OrderItemId,
                        domainResult.ErrorCode
                    );
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.OrderItem.MustBeRejectedToReturn)
                    );
                }

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = orderItem.OrderId,
                    EmployeeId = auditorId.Value,
                    Action = AuditLogActions.KdsReturn,
                    OldValue = $"\"{oldStatus}\"",
                    NewValue = $"\"{OrderItemStatus.Preparing}\"",
                    ChangeReason = "Manager returned item to queue",
                    CreatedAt = DateTime.UtcNow,
                };

                orderItemRepository.Update(orderItem);
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully returned OrderItemId: {OrderItemId} to queue",
                    request.OrderItemId
                );

                // SignalR Notify
                _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                    orderItem.OrderItemId,
                    OrderItemStatus.Preparing,
                    orderItem.StationSnapshot
                );

                var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
                var response = KdsMappingHelper.MapToResponse(orderItem, _priorityCalculator, settings);
                _ = _signalRService.NotifyKdsItemUpdatedAsync(orderItem.StationSnapshot, response);

                return Result<Guid>.Success(orderItem.OrderItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while returning OrderItemId to queue: {OrderItemId}",
                    request.OrderItemId
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

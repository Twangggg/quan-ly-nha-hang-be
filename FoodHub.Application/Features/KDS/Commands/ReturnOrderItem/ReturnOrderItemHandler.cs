using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
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
        private readonly ILogger<ReturnOrderItemHandler> _logger;

        public ReturnOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            ILogger<ReturnOrderItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            ReturnOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
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
                        _messageService.GetMessage(domainResult.ErrorCode!)
                    );
                }

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = orderItem.OrderId,
                    EmployeeId = Guid.Parse(_currentUserService.UserId!),
                    Action = AuditLogActions.KdsReturn,
                    OldValue = oldStatus.ToString(),
                    NewValue = OrderItemStatus.Preparing.ToString(),
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

using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.OrderItems.Commands.AdjustOrderItemQuantity
{
    public class AdjustOrderItemQuantityHandler
        : IRequestHandler<AdjustOrderItemQuantityCommand, Result<AdjustOrderItemQuantityResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AdjustOrderItemQuantityHandler> _logger;

        public AdjustOrderItemQuantityHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<AdjustOrderItemQuantityHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<AdjustOrderItemQuantityResponse>> Handle(
            AdjustOrderItemQuantityCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning(
                    "Unauthorized user attempt to adjust order item quantity for order {OrderId}.",
                    request.OrderId
                );
                return Result<AdjustOrderItemQuantityResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<AdjustOrderItemQuantityResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                _logger.LogWarning(
                    "Cannot adjust item quantity for Order {OrderId} because status is {Status}.",
                    order.OrderId,
                    order.Status
                );
                return Result<AdjustOrderItemQuantityResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );
            }

            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.OrderItemId == request.OrderItemId);
            if (orderItem == null)
            {
                return Result<AdjustOrderItemQuantityResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.OrderItem.NotFound),
                    ResultErrorType.NotFound
                );
            }

            var adjustResult = orderItem.AdjustQuantity(request.Quantity);
            if (!adjustResult.IsSuccess)
            {
                return Result<AdjustOrderItemQuantityResponse>.Failure(
                    _messageService.GetMessage(
                        adjustResult.ErrorCode ?? MessageKeys.OrderItem.InvalidQuantity
                    ),
                    ResultErrorType.BadRequest
                );
            }

            order.RecalculateTotalAmount();
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    EmployeeId = auditorId,
                    Action = AuditLogActions.AdjustOrderItemQuantity,
                    CreatedAt = DateTime.UtcNow,
                    ChangeReason = request.Reason,
                    NewValue = $"{{\"orderItemId\": \"{request.OrderItemId}\", \"newQuantity\": {request.Quantity}}}",
                };

                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
                _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                var response = _mapper.Map<AdjustOrderItemQuantityResponse>(order);
                return Result<AdjustOrderItemQuantityResponse>.Success(response);
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database error occurred while adjusting order item quantity for OrderId {OrderId}, OrderItemId {OrderItemId}",
                    request.OrderId,
                    request.OrderItemId
                );
                return Result<AdjustOrderItemQuantityResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Unexpected error occurred while adjusting order item quantity for OrderId {OrderId}, OrderItemId {OrderItemId}",
                    request.OrderId,
                    request.OrderItemId
                );
                throw;
            }
        }
    }
}

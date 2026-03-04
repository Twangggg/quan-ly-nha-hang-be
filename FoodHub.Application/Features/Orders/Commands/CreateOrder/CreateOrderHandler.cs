using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateOrderHandler> _logger;

        public CreateOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<CreateOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            CreateOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                "Creating new order. Type: {OrderType}, Table: {TableId}, CreatedBy: {UserId}",
                request.OrderType,
                request.TableId,
                userId
            );

            // Validate Basic Logic
            if (request.OrderType == OrderType.DineIn && request.TableId == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.SelectTable),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Generate Order Code inside transaction to prevent race condition
                var orderCode = await GenerateOrderCodeAsync(cancellationToken);

                // Create Order
                var newOrder = new Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderCode = orderCode,
                    OrderType = request.OrderType,
                    Status = OrderStatus.Serving,
                    TableId = request.OrderType == OrderType.DineIn ? request.TableId : null,
                    Note = request.Note,
                    TotalAmount = 0,
                    IsPriority = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                };

                await _unitOfWork.Repository<Order>().AddAsync(newOrder);

                // Audit Log — BR-12: required for "Tạo" action
                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = newOrder.OrderId,
                    EmployeeId = userId,
                    Action = AuditLogActions.CreateOrder,
                    NewValue =
                        $"{{\"orderCode\": \"{newOrder.OrderCode}\", \"orderType\": \"{newOrder.OrderType}\", \"tableId\": \"{newOrder.TableId}\"}}",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully created order {OrderCode} (Id: {OrderId})",
                    newOrder.OrderCode,
                    newOrder.OrderId
                );

                return Result<Guid>.Success(newOrder.OrderId);
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database error while creating order. Type: {OrderType}, Table: {TableId}",
                    request.OrderType,
                    request.TableId
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.Conflict
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Unexpected error while creating order. Type: {OrderType}, Table: {TableId}",
                    request.OrderType,
                    request.TableId
                );
                throw;
            }
        }

        /// <summary>
        /// Generate unique order code in format: ORD-yyyyMMdd-xxxx.
        /// Must be called inside a transaction to prevent race conditions.
        /// </summary>
        private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var dateString = today.ToString("yyyyMMdd");
            var prefix = $"ORD-{dateString}-";

            var lastOrder = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);

            int sequenceNumber = 1;
            if (lastOrder != null)
            {
                var parts = lastOrder.OrderCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                {
                    sequenceNumber = lastSequence + 1;
                }
            }

            return $"{prefix}{sequenceNumber:D4}";
        }
    }
}

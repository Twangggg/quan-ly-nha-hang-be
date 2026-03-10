using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CheckoutOrderHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public CheckoutOrderHandler(
            IUnitOfWork unitOfWork,
            ILogger<CheckoutOrderHandler> logger,
            IMessageService messageService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing checkout for OrderId: {OrderId}", request.OrderId);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Domain.Entities.Order>()
                .Query()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found for checkout. OrderId: {OrderId}", request.OrderId);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // Rich Domain Model: delegate business logic to entity
            var domainResult = order.Checkout(request.PaymentMethod, request.AmountReceived);
            if (!domainResult.IsSuccess)
            {
                _logger.LogWarning("Checkout failed for OrderId: {OrderId}. Reason: {ErrorCode}", request.OrderId, domainResult.ErrorCode);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(
                        domainResult.ErrorCode ?? MessageKeys.Order.InvalidAction
                    ),
                    ResultErrorType.BadRequest
                );
            }

            // Update Table to Cleaning if DineIn
            if (order.OrderType == OrderType.DineIn && order.TableId.HasValue)
            {
                var table = await _unitOfWork.Repository<Domain.Entities.Table>().GetByIdAsync(order.TableId.Value);
                if (table != null)
                {
                    table.Status = TableStatus.Cleaning;
                    _unitOfWork.Repository<Domain.Entities.Table>().Update(table);
                }
            }

            // Audit Log
            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = AuditLogActions.CheckoutOrder,
                CreatedAt = DateTime.UtcNow,
                NewValue = $"{{\"paymentMethod\": \"{request.PaymentMethod}\", \"totalAmount\": {order.TotalAmount}, \"amountPaid\": {order.AmountPaid}}}",
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while checking out OrderId {OrderId}", request.OrderId);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError)
                );
            }

            _logger.LogInformation("Successfully completed checkout for OrderId: {OrderId}", request.OrderId);

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

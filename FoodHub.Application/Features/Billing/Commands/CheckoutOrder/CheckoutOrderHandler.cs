using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
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

        public async Task<Result<Guid>> Handle(
            CheckoutOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Processing checkout for OrderId: {OrderId} with {LineCount} payment line(s)",
                request.OrderId, request.PaymentLines.Count);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found for checkout. OrderId: {OrderId}", request.OrderId);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // Validate total matches
            var totalPayment = request.PaymentLines.Sum(l => l.Amount);
            if (totalPayment != order.TotalAmount)
            {
                _logger.LogWarning("Split payment total mismatch. Expected: {Expected}, Got: {Got}",
                    order.TotalAmount, totalPayment);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Billing.SplitTotalMismatch),
                    ResultErrorType.BadRequest
                );
            }

            // Validate all payment method configs exist and are active
            var paymentMethodIds = request.PaymentLines.Select(l => l.PaymentMethodConfigId).Distinct().ToList();
            var paymentMethods = await _unitOfWork.Repository<PaymentMethodConfig>()
                .Query()
                .Where(pm => paymentMethodIds.Contains(pm.PaymentMethodConfigId))
                .ToListAsync(cancellationToken);

            if (paymentMethods.Count != paymentMethodIds.Count)
            {
                _logger.LogWarning("One or more PaymentMethodConfig not found");
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.PaymentMethodConfig.NotFound),
                    ResultErrorType.BadRequest
                );
            }

            var inactiveMethod = paymentMethods.FirstOrDefault(pm => !pm.IsActive);
            if (inactiveMethod != null)
            {
                _logger.LogWarning("PaymentMethodConfig {Id} is inactive", inactiveMethod.PaymentMethodConfigId);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.PaymentMethodConfig.Inactive),
                    ResultErrorType.BadRequest
                );
            }

            // Validate cash amount received
            foreach (var line in request.PaymentLines)
            {
                var method = paymentMethods.First(pm => pm.PaymentMethodConfigId == line.PaymentMethodConfigId);
                if (method.Type == PaymentMethodType.Cash && (line.AmountReceived ?? 0) < line.Amount)
                {
                    _logger.LogWarning("Insufficient cash amount for line {MethodId}", line.PaymentMethodConfigId);
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InsufficientAmount),
                        ResultErrorType.BadRequest
                    );
                }
            }

            // Determine legacy PaymentMethod from the first payment line type
            var primaryMethod = paymentMethods.First(pm =>
                pm.PaymentMethodConfigId == request.PaymentLines.First().PaymentMethodConfigId);
            var legacyPaymentMethod = primaryMethod.Type switch
            {
                PaymentMethodType.Cash => PaymentMethod.Cash,
                PaymentMethodType.BankTransfer => PaymentMethod.BankTransfer,
                _ => PaymentMethod.Cash
            };

            // Calculate total exactly received (supports split payments including cash over-payment)
            var totalReceived = request.PaymentLines.Sum(l => 
            {
                var m = paymentMethods.First(pm => pm.PaymentMethodConfigId == l.PaymentMethodConfigId);
                return m.Type == PaymentMethodType.Cash ? (l.AmountReceived ?? l.Amount) : l.Amount;
            });

            var domainResult = order.ProcessCheckout(legacyPaymentMethod, totalReceived);
            if (!domainResult.IsSuccess)
            {
                _logger.LogWarning("Checkout failed for OrderId: {OrderId}. Reason: {ErrorCode}",
                    request.OrderId, domainResult.ErrorCode);

                if (domainResult.ErrorCode == DomainErrors.Order.InvalidActionWithStatus)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(
                            MessageKeys.Order.InvalidActionWithStatus,
                            new { Status = order.Status.ToString() }
                        ),
                        ResultErrorType.BadRequest
                    );
                }

                return Result<Guid>.Failure(
                    _messageService.GetMessage(
                        domainResult.ErrorCode ?? MessageKeys.Order.InvalidAction
                    ),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Create OrderPayment records for each payment line
                foreach (var line in request.PaymentLines)
                {
                    var orderPayment = new OrderPayment
                    {
                        OrderPaymentId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        PaymentMethodConfigId = line.PaymentMethodConfigId,
                        Amount = line.Amount,
                        PaidAt = DateTime.UtcNow,
                        CreatedBy = auditorId,
                    };
                    await _unitOfWork.Repository<OrderPayment>().AddAsync(orderPayment);
                }

                // Audit Log
                var paymentSummary = string.Join(", ",
                    request.PaymentLines.Select(l =>
                    {
                        var m = paymentMethods.First(pm => pm.PaymentMethodConfigId == l.PaymentMethodConfigId);
                        return $"{m.Name}: {l.Amount}";
                    }));

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    EmployeeId = auditorId,
                    Action = AuditLogActions.CheckoutOrder,
                    CreatedAt = DateTime.UtcNow,
                    NewValue = $"{{\"payments\": \"{paymentSummary}\", \"totalAmount\": {order.TotalAmount}}}",
                };

                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
                _unitOfWork.Repository<Order>().Update(order);

                // Update Table to Available if DineIn
                if (order.OrderType == OrderType.DineIn && order.TableId.HasValue)
                {
                    var table = await _unitOfWork
                        .Repository<Table>()
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(t => t.TableId == order.TableId, cancellationToken);
                    if (table != null)
                    {
                        if (table.SetAvailable())
                        {
                            table.UpdatedAt = DateTime.UtcNow;
                        }
                        _unitOfWork.Repository<Table>().Update(table);

                        order.TableId = null;
                        _unitOfWork.Repository<Order>().Update(order);
                    }
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Transaction failed while checking out OrderId: {OrderId}",
                    request.OrderId
                );
                throw;
            }

            _logger.LogInformation(
                "Successfully completed checkout for OrderId: {OrderId} with {LineCount} payment line(s)",
                request.OrderId, request.PaymentLines.Count
            );

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

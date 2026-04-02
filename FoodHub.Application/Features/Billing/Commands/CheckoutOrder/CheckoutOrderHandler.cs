using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CheckoutOrderHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ISignalRService _signalRService;

        public CheckoutOrderHandler(
            IUnitOfWork unitOfWork,
            ILogger<CheckoutOrderHandler> logger,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            ISignalRService signalRService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _signalRService = signalRService;
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
                _logger.LogWarning(
                    "Order not found for checkout. OrderId: {OrderId}",
                    request.OrderId
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // Tính số tiền còn lại cần thanh toán
            var remainingAmount = order.GetRemainingAmount();
            var totalPayment = request.PaymentLines.Sum(l => l.Amount);

            if (totalPayment <= 0)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Billing.SplitTotalMismatch),
                    ResultErrorType.BadRequest
                );
            }

            // Không cho phép thanh toán vượt quá số tiền còn lại
            if (totalPayment > remainingAmount)
            {
                _logger.LogWarning("Payment exceeds remaining amount. Remaining: {Remaining}, Got: {Got}",
                    remainingAmount, totalPayment);
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

            // Ghi nhận thanh toán một phần (cộng dồn AmountPaid)
            var domainResult = order.AddCashPayment(totalPayment);
            if (!domainResult.IsSuccess)
            {
                _logger.LogWarning(
                    "AddCashPayment failed for OrderId: {OrderId}. Reason: {ErrorCode}",
                    request.OrderId,
                    domainResult.ErrorCode
                );

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

            // Nếu đã thanh toán đủ => hoàn tất đơn
            var isFullyPaid = order.GetRemainingAmount() <= 0;
            if (isFullyPaid)
            {
                var completeResult = order.CompletePayment();
                if (!completeResult.IsSuccess)
                {
                    _logger.LogWarning("CompletePayment failed for OrderId: {OrderId}", request.OrderId);
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                        ResultErrorType.BadRequest
                    );
                }
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
                    NewValue = JsonSerializer.Serialize(
                        new
                        {
                            payments = paymentSummary,
                            totalAmount = order.TotalAmount,
                            amountPaid = order.AmountPaid,
                            isPartial = !isFullyPaid,
                        }
                    ),
                };

                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
                _unitOfWork.Repository<Order>().Update(order);

                // Chỉ giải phóng bàn khi đã thanh toán đủ
                if (isFullyPaid && order.OrderType == OrderType.DineIn && order.TableId.HasValue)
                {
                    var tableIdSnapshot = order.TableId.Value;
                    var table = await _unitOfWork
                        .Repository<Table>()
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(t => t.TableId == order.TableId, cancellationToken);
                    if (table != null)
                    {
                        var statusChanged = table.SetAvailable();
                        if (statusChanged)
                        {
                            table.UpdatedAt = DateTime.UtcNow;
                            table.UpdatedBy = auditorId;
                        }
                        _unitOfWork.Repository<Table>().Update(table);

                        // Cập nhật Reservation sang Completed
                        if (order.ReservationId.HasValue)
                        {
                            var reservation = await _unitOfWork
                                .Repository<Reservation>()
                                .GetByIdAsync(order.ReservationId.Value);
                            if (reservation != null)
                            {
                                reservation.Complete(DateTime.UtcNow, auditorId);
                                _unitOfWork.Repository<Reservation>().Update(reservation);
                            }
                        }

                        // Ngắt kết nối đơn hàng với bàn sau khi đã giải phóng bàn xong
                        order.ReleaseTable();
                        _unitOfWork.Repository<Order>().Update(order);

                        await _signalRService.NotifyTableStatusChangedAsync(
                            tableIdSnapshot,
                            table.Status.ToString()
                        );
                    }
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(
                    CacheKey.TableList + "*",
                    cancellationToken
                );
                await _cacheService.RemoveByPatternAsync(
                    string.Format(CacheKey.TableListByArea, "*"),
                    cancellationToken
                );

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
                "Successfully processed {PaymentType} payment for OrderId: {OrderId}. AmountPaid: {AmountPaid}, Remaining: {Remaining}",
                isFullyPaid ? "full" : "partial",
                request.OrderId,
                order.AmountPaid,
                order.GetRemainingAmount()
            );

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

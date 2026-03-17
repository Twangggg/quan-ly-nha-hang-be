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
            ICurrentUserService currentUserService
        )
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

            // Rich Domain Model: delegate business logic to entity
            var domainResult = order.ProcessCheckout(request.PaymentMethod, request.AmountPaid);
            if (!domainResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Checkout failed for OrderId: {OrderId}. Reason: {ErrorCode}",
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Audit Log
                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    EmployeeId = auditorId,
                    Action = AuditLogActions.CheckoutOrder,
                    CreatedAt = DateTime.UtcNow,
                    NewValue =
                        $"{{\"paymentMethod\": \"{request.PaymentMethod}\", \"totalAmount\": {order.TotalAmount}, \"amountPaid\": {order.AmountPaid}}}",
                };

                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
                _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

                // Update Table to Cleaning if DineIn
                if (order.OrderType == OrderType.DineIn && order.TableId.HasValue)
                {
                    var table = await _unitOfWork
                        .Repository<Domain.Entities.Table>()
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(t => t.TableId == order.TableId, cancellationToken);
                    if (table != null)
                    {
                        if (table.SetAvailable())
                        {
                            table.UpdatedAt = DateTime.UtcNow;
                        }
                        //table.MarkAsAvailable();
                        _unitOfWork.Repository<Domain.Entities.Table>().Update(table);

                        // Cập nhật Reservation sang Completed
                        if (order.ReservationId.HasValue)
                        {
                            var reservation = await _unitOfWork
                                .Repository<Reservation>()
                                .GetByIdAsync(order.ReservationId.Value);
                            if (reservation != null)
                            {
                                reservation.Status = ReservationStatus.Completed;
                                reservation.UpdatedAt = DateTime.UtcNow;
                                reservation.UpdatedBy = auditorId;
                                _unitOfWork.Repository<Reservation>().Update(reservation);
                            }
                        }

                        // Chú ý: Tại đây nếu một nhóm khách ngồi nhiều bàn (đã gộp đơn về đơn này), 
                        // nhân viên sẽ cần giải phóng các bàn kia thủ công hoặc chúng ta cần lưu vết liên kết đa bàn.
                        // Hiện tại hệ thống ưu tiên tính minh bạch: giải phóng bàn chính gắn với đơn hàng.

                        // Ngắt kết nối đơn hàng với bàn sau khi đã giải phóng bàn xong
                        order.TableId = null;
                        _unitOfWork.Repository<Domain.Entities.Order>().Update(order);
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
                "Successfully completed checkout for OrderId: {OrderId}",
                request.OrderId
            );

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

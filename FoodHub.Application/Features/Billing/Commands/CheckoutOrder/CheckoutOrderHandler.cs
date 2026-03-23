using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
                    var tableIdSnapshot = order.TableId.Value; // Capture before nulling
                    var table = await _unitOfWork
                        .Repository<Domain.Entities.Table>()
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(t => t.TableId == order.TableId, cancellationToken);
                    if (table != null)
                    {
                        var statusChanged = table.SetAvailable();
                        if (statusChanged)
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
                    
                        if (table != null)
                        {
                            await _signalRService.NotifyTableStatusChangedAsync(tableIdSnapshot, table.Status.ToString());
                        }
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
                "Successfully completed checkout for OrderId: {OrderId}",
                request.OrderId
            );

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Commands.SyncPaymentStatus
{
    public class SyncPaymentStatusCommand : IRequest<Result<bool>>
    {
        public Guid OrderId { get; set; }
    }

    public class SyncPaymentStatusHandler : IRequestHandler<SyncPaymentStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<SyncPaymentStatusHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly ISignalRService _signalRService;
        private readonly IMessageService _messageService;

        public SyncPaymentStatusHandler(
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            ILogger<SyncPaymentStatusHandler> logger,
            ICacheService cacheService,
            ISignalRService signalRService,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _logger = logger;
            _cacheService = cacheService;
            _signalRService = signalRService;
            _messageService = messageService;
        }

        public async Task<Result<bool>> Handle(
            SyncPaymentStatusCommand request,
            CancellationToken cancellationToken
        )
        {
            var order = await _unitOfWork
                .Repository<Order>()
                .Query()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null || order.TransactionCode == 0)
            {
                return Result<bool>.Failure(
                    MessageKeys.Billing.OrderOrPaymentNotFound,
                    ResultErrorType.NotFound
                );
            }

            if (order.Status == OrderStatus.Paid)
            {
                return Result<bool>.Success(true);
            }

            // Call PayOS directly to check status
            var status = await _paymentService.GetPaymentStatusAsync(
                order.TransactionCode,
                cancellationToken
            );

            if (status != "PAID")
            {
                return Result<bool>.Success(false); // Not paid yet
            }

            // Match logic from Webhook handler
            var lockKey = $"payos_webhook_lock_{order.TransactionCode}";
            var isLocked = await _cacheService.ExistsAsync(lockKey, cancellationToken);
            if (isLocked)
            {
                return Result<bool>.Success(true);
            }

            await _cacheService.SetAsync(
                lockKey,
                "locked",
                TimeSpan.FromSeconds(10),
                cancellationToken
            );

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Re-fetch to ensure nothing changed
                var reloadOrder = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

                if (reloadOrder == null || reloadOrder.Status == OrderStatus.Paid)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<bool>.Success(true);
                }

                var domainResult = reloadOrder.Checkout(
                    PaymentMethod.QRCode,
                    reloadOrder.TotalAmount
                );
                if (!domainResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<bool>.Failure(
                        domainResult.ErrorCode ?? MessageKeys.Common.CheckoutFailed,
                        ResultErrorType.BadRequest
                    );
                }

                // Release table
                if (reloadOrder.OrderType == OrderType.DineIn && reloadOrder.TableId.HasValue)
                {
                    var table = await _unitOfWork
                        .Repository<Table>()
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(
                            t => t.TableId == reloadOrder.TableId.Value,
                            cancellationToken
                        );

                    if (table != null)
                    {
                        table.SetAvailable();
                        _unitOfWork.Repository<Table>().Update(table);
                        reloadOrder.TableId = null;
                    }
                }

                _unitOfWork.Repository<Order>().Update(reloadOrder);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Clear cache
                await _cacheService.RemoveByPatternAsync(
                    CacheKey.TableList + "*",
                    cancellationToken
                );

                await _unitOfWork.CommitTransactionAsync();

                // Notify via SignalR
                await _signalRService.NotifyOrderStatusChangedAsync(
                    reloadOrder.OrderId,
                    reloadOrder.Status.ToString()
                );

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Error syncing payment status for order {Id}",
                    request.OrderId
                );
                throw;
            }
        }
    }
}

using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Commands.ProcessPaymentWebhook
{
    public class ProcessPaymentWebhookHandler
        : IRequestHandler<ProcessPaymentWebhookCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<ProcessPaymentWebhookHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly ISignalRService _signalRService;

        public ProcessPaymentWebhookHandler(
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            ILogger<ProcessPaymentWebhookHandler> logger,
            ICacheService cacheService,
            ISignalRService signalRService
        )
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _logger = logger;
            _cacheService = cacheService;
            _signalRService = signalRService;
        }

        public async Task<Result<bool>> Handle(
            ProcessPaymentWebhookCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Processing PayOS Webhook...");

            long orderCode;
            try
            {
                orderCode = await _paymentService.VerifyWebhookDataAsync(request.WebhookBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid PayOS Webhook Signature");
                return Result<bool>.Failure("Invalid Signature", ResultErrorType.BadRequest);
            }

            // PayOS test webhook validation handler
            if (orderCode == 123)
            {
                _logger.LogInformation("Received Test Webhook from PayOS. Returning 200 OK.");
                return Result<bool>.Success(true);
            }

            var lockKey = $"payos_webhook_lock_{orderCode}";
            var isLocked = await _cacheService.ExistsAsync(lockKey, cancellationToken);
            if (isLocked)
            {
                _logger.LogWarning(
                    "Webhook for OrderCode {OrderCode} is already being processed.",
                    orderCode
                );
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

                var order = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .FirstOrDefaultAsync(o => o.TransactionCode == orderCode, cancellationToken);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogWarning(
                        "Order with TransactionCode {OrderCode} not found for webhook.",
                        orderCode
                    );
                    return Result<bool>.Success(true);
                }

                if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogInformation(
                        "Order {OrderId} already Paid/Cancelled.",
                        order.OrderId
                    );
                    return Result<bool>.Success(true);
                }

                var domainResult = order.Checkout(PaymentMethod.QRCode, order.TotalAmount);
                if (!domainResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogWarning(
                        "Order {OrderId} checkout failed via webhook: {Error}",
                        order.OrderId,
                        domainResult.ErrorCode
                    );
                    return Result<bool>.Success(true);
                }

                if (order.OrderType == OrderType.DineIn && order.TableId.HasValue)
                {
                    var table = await _unitOfWork
                        .Repository<Table>()
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(
                            t => t.TableId == order.TableId.Value,
                            cancellationToken
                        );

                    if (table != null)
                    {
                        if (table.SetAvailable())
                        {
                            table.UpdatedAt = DateTime.UtcNow;
                        }
                        _unitOfWork.Repository<Table>().Update(table);

                        order.TableId = null;
                    }
                }

                _unitOfWork.Repository<Order>().Update(order);
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

                await _signalRService.NotifyOrderStatusChangedAsync(
                    order.OrderId,
                    order.Status.ToString()
                );

                _logger.LogInformation(
                    "Successfully processed webhook for OrderId: {OrderId}",
                    order.OrderId
                );
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Transaction failed while processing webhook for OrderCode: {OrderCode}",
                    orderCode
                );
                throw;
            }
            finally
            {
                // Lock will expire automatically to prevent blocking if something fails and we need to retry later.
            }
        }
    }
}

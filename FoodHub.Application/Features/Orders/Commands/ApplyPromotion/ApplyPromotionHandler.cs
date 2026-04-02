using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.ApplyPromotion
{
    public class ApplyPromotionHandler
        : IRequestHandler<ApplyPromotionCommand, Result<ApplyPromotionResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ApplyPromotionHandler> _logger;
        private readonly IMessageService _messageService;

        public ApplyPromotionHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<ApplyPromotionHandler> logger,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<ApplyPromotionResponse>> Handle(
            ApplyPromotionCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Attempting to apply promotion {Code} to order {OrderId}",
                request.Code,
                request.OrderId
            );

            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<ApplyPromotionResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized
                );
            }

            var orderRepo = _unitOfWork.Repository<Order>();
            var promotionRepo = _unitOfWork.Repository<Promotion>();
            var orderItemRepo = _unitOfWork.Repository<OrderItem>();

            var order = await orderRepo
                .Query()
                .Include(o => o.OrderItems)
                .Include(o => o.Promotion)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                return Result<ApplyPromotionResponse>.Failure(
                    _messageService.GetMessage(DomainErrors.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            var promotion = await promotionRepo
                .Query()
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.Code == request.Code, cancellationToken);

            if (promotion == null)
            {
                _logger.LogWarning("Promotion {Code} not found", request.Code);
                return Result<ApplyPromotionResponse>.Failure(
                    _messageService.GetMessage(DomainErrors.Promotion.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // Begin Transaction for multi-write (Order update + Promotion UsedCount)
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Domain validation
                var validation = promotion.Validate(
                    order.GetPromotionValidationSubTotal(),
                    DateTimeOffset.UtcNow
                );
                if (!validation.IsSuccess)
                {
                    _logger.LogWarning(
                        "Promotion {Code} validation failed: {ErrorCode}",
                        request.Code,
                        validation.ErrorCode
                    );
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<ApplyPromotionResponse>.Failure(
                        _messageService.GetMessage(validation.ErrorCode!)
                    );
                }

                // Remove existing free items when switching promotions
                var existingFreeItems = order.OrderItems.Where(oi => oi.IsFreeItem).ToList();

                // Validation: If any gift item is already cooking or completed, prevent switching voucher
                if (existingFreeItems.Any(fi => fi.Status != OrderItemStatus.Preparing))
                {
                    _logger.LogWarning(
                        "Cannot switch promotion for order {OrderCode} because gift items are already in process",
                        order.OrderCode
                    );
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<ApplyPromotionResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Voucher.GiftInProcess)
                    );
                }

                foreach (var freeItem in existingFreeItems)
                {
                    orderItemRepo.Delete(freeItem);
                }

                // If existing promotion, decrement its usage (if it's a different one)
                if (order.PromotionId.HasValue && order.PromotionId != promotion.PromotionId)
                {
                    order.Promotion?.DecrementUsed(userId);
                }

                var oldPromotionId = order.PromotionId;
                var oldPromotionCode = order.Promotion?.Code;

                // Apply promotion to order
                order.ApplyPromotion(promotion, userId);
                promotion.IncrementUsed(userId);

                // Handle FreeItem promotion: create a free OrderItem
                if (
                    promotion.Type == PromotionType.FreeItem
                    && promotion.ItemId.HasValue
                    && promotion.Item != null
                )
                {
                    var freeOrderItem = new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        MenuItemId = promotion.ItemId.Value,
                        ItemCodeSnapshot = promotion.Item.Code,
                        ItemNameSnapshot = promotion.Item.Name,
                        StationSnapshot = promotion.Item.Station.ToString(),
                        Quantity = promotion.FreeQuantity ?? 1,
                        UnitPriceSnapshot = promotion.Item.Price,
                        IsFreeItem = true,
                        Status = OrderItemStatus.Preparing,
                        CreatedAt = DateTime.UtcNow,
                    };

                    // This order already exists in the database, so explicitly register
                    // the gifted item as Added. Relying on navigation fixup alone can
                    // leave EF treating it as an update against a non-existent row.
                    await orderItemRepo.AddAsync(freeOrderItem);

                    _logger.LogInformation(
                        "Added free item {ItemName} x{Qty} to order {OrderCode} via FreeItem promotion {Code}",
                        promotion.Item.Name,
                        freeOrderItem.Quantity,
                        order.OrderCode,
                        promotion.Code
                    );
                }

                // All entities (order, promotion, old promotion) were loaded via
                // tracked queries on the same DbContext, so their in-memory mutations
                // are already detected by EF's change tracker.
                // Calling _dbSet.Update() here would walk the entire entity graph and
                // produce tracking-state conflicts — so we intentionally omit it.

                // Add audit log
                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    EmployeeId = userId,
                    Action = "ApplyPromotion",
                    NewValue = System.Text.Json.JsonSerializer.Serialize(new { 
                        PromotionCode = promotion.Code, 
                        DiscountAmount = order.DiscountAmount 
                    }),
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully applied promotion {Code} to order {OrderCode}. Discount: {Amount}",
                    promotion.Code,
                    order.OrderCode,
                    order.DiscountAmount
                );

                return Result<ApplyPromotionResponse>.Success(
                    new ApplyPromotionResponse
                    {
                        OrderId = order.OrderId,
                        OrderCode = order.OrderCode,
                        OldPromotionId = oldPromotionId,
                        OldPromotionCode = oldPromotionCode,
                        NewPromotionId = order.PromotionId,
                        NewPromotionCode = order.Promotion?.Code,
                        SubTotal = order.SubTotal,
                        DiscountAmount = order.DiscountAmount,
                        VatAmount = order.VatAmount,
                        TotalAmount = order.TotalAmount,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Error applying promotion {Code} to order {OrderId}. Inner: {Inner}",
                    request.Code,
                    request.OrderId,
                    ex.InnerException?.Message
                );
                return Result<ApplyPromotionResponse>.Failure(
                    $"Error: {ex.Message} | Inner: {ex.InnerException?.Message}",
                    ResultErrorType.Conflict
                );
            }
        }
    }
}

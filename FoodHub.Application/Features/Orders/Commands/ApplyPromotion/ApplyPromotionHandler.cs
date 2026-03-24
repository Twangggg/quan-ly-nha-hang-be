using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.ApplyPromotion
{
    public class ApplyPromotionHandler : IRequestHandler<ApplyPromotionCommand, Result<ApplyPromotionResponse>>
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
                    "User not authenticated",
                    ResultErrorType.Unauthorized
                );
            }

            var orderRepo = _unitOfWork.Repository<Order>();
            var promotionRepo = _unitOfWork.Repository<Promotion>();

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

                orderRepo.Update(order);
                promotionRepo.Update(promotion);

                // Add audit log
                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    EmployeeId = userId,
                    Action = "ApplyPromotion",
                    NewValue =
                        $"{{\"PromotionCode\": \"{promotion.Code}\", \"DiscountAmount\": {order.DiscountAmount}}}",
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
                    "Error applying promotion {Code} to order {OrderId}",
                    request.Code,
                    request.OrderId
                );
                return Result<ApplyPromotionResponse>.Failure(
                    _messageService.GetMessage("Common.DatabaseUpdateError"),
                    ResultErrorType.Conflict
                );
            }
        }
    }
}

using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Orders.Commands.UnapplyPromotion
{
    public class UnapplyPromotionHandler : IRequestHandler<UnapplyPromotionCommand, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public UnapplyPromotionHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<Unit>> Handle(
            UnapplyPromotionCommand request,
            CancellationToken cancellationToken
        )
        {
            var orderRepo = _unitOfWork.Repository<Order>();
            var promotionRepo = _unitOfWork.Repository<Promotion>();
            var orderItemRepo = _unitOfWork.Repository<OrderItem>();

            var order = await orderRepo
                .Query()
                .Include(o => o.OrderItems)
                .Include(o => o.Promotion)
                    .ThenInclude(p => p!.Item)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order is null)
            {
                return Result<Unit>.Failure(
                    _messageService.GetMessage(DomainErrors.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (!order.PromotionId.HasValue || order.Promotion is null)
            {
                return Result<Unit>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.VoucherNotApplied),
                    ResultErrorType.BadRequest
                );
            }

            Guid? userId = Guid.TryParse(_currentUserService.UserId, out var parsedUserId)
                ? parsedUserId
                : null;

            var promotion = order.Promotion;
            var promotionCode = promotion.Code;
            var discountAmount = order.DiscountAmount;

            if (promotion.Type == Domain.Enums.PromotionType.FreeItem && promotion.ItemId.HasValue)
            {
                var freeItems = order
                    .OrderItems.Where(oi =>
                        oi.IsFreeItem && oi.MenuItemId == promotion.ItemId.Value
                    )
                    .ToList();

                // Chặn nếu món tặng đã bắt đầu nấu
                if (freeItems.Any(fi => fi.Status != OrderItemStatus.Preparing))
                {
                    return Result<Unit>.Failure(
                        _messageService.GetMessage(MessageKeys.Voucher.GiftInProcess)
                    );
                }

                foreach (var freeItem in freeItems)
                {
                    orderItemRepo.Delete(freeItem);
                }
            }

            promotion.DecrementUsed(userId ?? Guid.Empty);

            order.Promotion = null;
            order.PromotionId = null;
            order.DiscountAmount = 0;
            order.RecalculateTotalAmount();
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = userId;

            // Entities are already tracked. Calling Update() is largely redundant and can cause tracking conflicts
            // orderRepo.Update(order);
            // promotionRepo.Update(promotion);

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = userId ?? Guid.Empty,
                Action = "UnapplyPromotion",
                OldValue = System.Text.Json.JsonSerializer.Serialize(new { 
                    PromotionCode = promotionCode, 
                    DiscountAmount = discountAmount 
                }),
                NewValue = System.Text.Json.JsonSerializer.Serialize(new { 
                    PromotionCode = (string?)null, 
                    DiscountAmount = order.DiscountAmount 
                }),
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}

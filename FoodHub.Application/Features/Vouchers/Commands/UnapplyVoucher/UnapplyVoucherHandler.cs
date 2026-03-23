using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Features.Vouchers.Commands.ApplyVoucher;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Vouchers.Commands.UnapplyVoucher
{
    public class UnapplyVoucherHandler : IRequestHandler<UnapplyVoucherCommand, Result<UnapplyVoucherResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApplyVoucherHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public UnapplyVoucherHandler(
            IUnitOfWork unitOfWork,
            ILogger<ApplyVoucherHandler> logger,
            IMessageService messageService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UnapplyVoucherResponse>> Handle(UnapplyVoucherCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UnapplyVoucherCommand for OrderId: {OrderId}", request.OrderId);

            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();

            var orderRepository = _unitOfWork.Repository<Order>();
            var orderItemRepository = _unitOfWork.Repository<OrderItem>();
            var voucherRepository = _unitOfWork.Repository<Voucher>();

            // Kiểm tra sự tồn tại của order
            // Hiện tại chưa kiểm tra trạng thái của order
            var order = await orderRepository
                .Query()
                .Include(o => o.Voucher)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OptionGroups)
                .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
            if (order == null)
            {
                _logger.LogWarning("Order with Id {OrderId} not found", request.OrderId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound);
                return Result<UnapplyVoucherResponse>.NotFound(errorMessage);
            }

            // Nếu order đã có voucher rồi thì gỡ voucher cũ ra trước, sau đó mới áp voucher mới vào,
            // tránh trường hợp gỡ voucher cũ rồi nhưng áp voucher mới không thành công
            // thì order lại bị mất voucher cũ
            var oldVoucher = order.Voucher;
            if (oldVoucher == null)
            {
                _logger.LogWarning("Order with Id {OrderId} does not have a voucher to unapply", request.OrderId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFound);
                return Result<UnapplyVoucherResponse>.NotFound(errorMessage);
            }

            // Loại bỏ free item cũ nếu có
            var freeItemInOrder = order.OrderItems.FirstOrDefault(oi => oi.IsFreeItem);
            if (freeItemInOrder != null)
            {
                _logger.LogInformation("Removing free item with OrderItemId {OrderItemId} from OrderId {OrderId} due to voucher unapplied", freeItemInOrder.OrderItemId, order.OrderId);
                
                // Tìm kiếm item gốc cùng loại món ăn (MenuItemId) nhưng không phải là FreeItem
                var notFreeItem = order.OrderItems.FirstOrDefault(oi => 
                    oi.MenuItemId == freeItemInOrder.MenuItemId 
                    && !oi.IsFreeItem 
                    && oi.OrderItemId != freeItemInOrder.OrderItemId);

                if (notFreeItem != null)
                {
                    _logger.LogInformation("Found non-free item with OrderItemId {OrderItemId} in OrderId {OrderId}, increasing quantity by {Quantity}", notFreeItem.OrderItemId, order.OrderId, freeItemInOrder.Quantity);
                    
                    notFreeItem.IncreaseQuantity(freeItemInOrder.Quantity, DateTime.UtcNow);
                    orderItemRepository.Update(notFreeItem);
                    orderItemRepository.Delete(freeItemInOrder);
                }
                else
                {
                    _logger.LogInformation("No non-free item found. Marking free item {OrderItemId} as not free", freeItemInOrder.OrderItemId);
                    freeItemInOrder.IsFreeItem = false;
                    orderItemRepository.Update(freeItemInOrder);
                }
                
            }

            _logger.LogInformation("Unapplying voucher with Id {VoucherId} from OrderId {OrderId}", oldVoucher.VoucherId, order.OrderId);
            order.ApplyVoucher(null, auditorId);
            oldVoucher.UnUsed(auditorId);

            orderRepository.Update(order);
            if (oldVoucher != null)
            {
                _logger.LogInformation("Updating voucher with Id {VoucherId} to unused status after unapplied from OrderId {OrderId}", oldVoucher.VoucherId, order.OrderId);
                voucherRepository.Update(oldVoucher);
            }
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new UnapplyVoucherResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                OldVoucherId = oldVoucher?.VoucherId ?? Guid.Empty,
                OldVoucherCode = oldVoucher?.VoucherCode ?? string.Empty,
                TotalAmount = order.TotalAmount
            };

            return Result<UnapplyVoucherResponse>.Success(response);
        }
    }
}

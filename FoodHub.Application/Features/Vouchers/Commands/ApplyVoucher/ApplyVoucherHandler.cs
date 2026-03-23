using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Vouchers.Commands.ApplyVoucher
{
    public class ApplyVoucherHandler : IRequestHandler<ApplyVoucherCommand, Result<ApplyVoucherResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApplyVoucherHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public ApplyVoucherHandler(
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

        public async Task<Result<ApplyVoucherResponse>> Handle(ApplyVoucherCommand request, CancellationToken cancellationToken)
        {
            // Dự kiến workflow: khách vào bàn -> order -> thêm món -> (gôp tách order) -> áp voucher -> thanh toán
            // Vây nên trạng thái order có khi không nhất quán,
            // tạm thời không kiểm tra trạng thái của order,
            // Sau này nếu có yêu cầu thêm về trạng thái order thì sẽ bổ sung sau,
            // hiện tại chưa muốn check cứng trạng thái order ở đây để tránh trường hợp sau này có thay đổi về
            // workflow order thì lại phải sửa lại phần apply voucher này.

            _logger.LogInformation("Handling ApplyVoucherCommand for OrderId: {OrderId}, VoucherId: {VoucherId}", request.OrderId, request.VoucherId);

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
                _logger.LogInformation("Order not found for OrderId: {OrderId}", request.OrderId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound);
                return Result<ApplyVoucherResponse>.NotFound(errorMessage);
            }

            // Kiểm tra sự tồn tại của voucher
            var newVoucher = await voucherRepository
                .Query()
                .Include(v => v.Item)
                .FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId, cancellationToken);
            if (newVoucher == null)
            {
                _logger.LogInformation("Voucher not found for VoucherId: {VoucherId}", request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFound);
                return Result<ApplyVoucherResponse>.NotFound(errorMessage);
            }
            if (!newVoucher.IsValid())
            {
                _logger.LogInformation("Voucher is not valid for VoucherId: {VoucherId}", request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.Invalid);
                return Result<ApplyVoucherResponse>.Failure(errorMessage);
            }

            // Nếu order đã có voucher rồi thì gỡ voucher cũ ra trước, sau đó mới áp voucher mới vào,
            // tránh trường hợp gỡ voucher cũ rồi nhưng áp voucher mới không thành công
            // thì order lại bị mất voucher cũ
            var oldVoucher = order.Voucher;
            if (oldVoucher != null)
            {
                //order.ApplyVoucher(null, auditorId);

                // Kiểm tra xem voucher đã được áp dụng cho order này chưa, nếu rồi thì trả lỗi luôn, không cần gỡ voucher cũ ra rồi áp lại, tránh trường hợp gỡ voucher cũ ra rồi nhưng áp voucher mới không thành công thì order lại bị mất voucher cũ
                if (oldVoucher.VoucherId == request.VoucherId)
                {
                    _logger.LogInformation("Voucher already applied for OrderId: {OrderId}, VoucherId: {VoucherId}", request.OrderId, request.VoucherId);
                    var errorMessage = _messageService.GetMessage(MessageKeys.Order.VoucherAlreadyApplied);
                    return Result<ApplyVoucherResponse>.Failure(errorMessage);
                }
            }

            // Kiểm tra voucher mới có phải là free item trong order không, nếu có thì mới áp được, nếu không thì trả lỗi, tránh trường hợp áp voucher vào rồi nhưng voucher không hợp lệ thì lại phải gỡ voucher
            if (newVoucher.VoucherType == VoucherType.FreeItem && !newVoucher.IsFreeItemInOrder(order))
            {
                //order.ApplyVoucher(oldVoucher, auditorId);

                _logger.LogInformation("Voucher is not free item in order for OrderId: {OrderId}, VoucherId: {VoucherId}", request.OrderId, request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFreeItemInOrder);
                return Result<ApplyVoucherResponse>.Failure(errorMessage);
            }
            // Kiểm tra xem order tổng có hợp lệ với voucher mới không, nếu không thì trả lỗi, tránh trường hợp áp voucher vào rồi nhưng voucher không hợp lệ thì lại phải gỡ voucher
            if (newVoucher.IsBelowMinAmount(order.SubTotal))
            {
                //order.ApplyVoucher(oldVoucher, auditorId);

                _logger.LogInformation("Voucher is not total amount valid for OrderId: {OrderId}, VoucherId: {VoucherId}", request.OrderId, request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.BelowMinAmount);
                return Result<ApplyVoucherResponse>.Failure(errorMessage);
            }

            // Loại bỏ free item cũ nếu có, thêm free item mới vào nếu có, sau đó mới áp voucher vào order, tránh trường hợp áp voucher vào rồi nhưng voucher không hợp lệ thì lại phải gỡ voucher
            var freeItemInOrder = order.OrderItems.FirstOrDefault(oi => oi.IsFreeItem);
            if (freeItemInOrder != null)
            {
                _logger.LogInformation("Removing old free item from order for OrderId: {OrderId}, FreeItemOrderItemId: {FreeItemOrderItemId}", request.OrderId, freeItemInOrder.OrderItemId);
                
                var notFreeItem = order.OrderItems.FirstOrDefault(oi => 
                    oi.MenuItemId == freeItemInOrder.MenuItemId 
                    && !oi.IsFreeItem 
                    && oi.OrderItemId != freeItemInOrder.OrderItemId);

                if (notFreeItem != null)
                {
                    _logger.LogInformation("Found not free item for old free item in order for OrderId: {OrderId}, NotFreeItemOrderItemId: {NotFreeItemOrderItemId}", request.OrderId, notFreeItem.OrderItemId);
                    
                    notFreeItem.IncreaseQuantity(freeItemInOrder.Quantity, DateTime.UtcNow);
                    orderItemRepository.Update(notFreeItem);
                    orderItemRepository.Delete(freeItemInOrder);
                }
                else
                {
                    _logger.LogInformation("No not free item found for old free item in order for OrderId: {OrderId}. Marking as not free.", request.OrderId);
                    freeItemInOrder.IsFreeItem = false;
                    orderItemRepository.Update(freeItemInOrder);
                }
            }

            // Nếu voucher mới có free item thì thêm free item vào order
            if (newVoucher.VoucherType == VoucherType.FreeItem)
            {
                _logger.LogInformation("Adding new free item to order for OrderId: {OrderId}, VoucherId: {VoucherId}, FreeItemMenuItemId: {FreeItemMenuItemId}, FreeQuantity: {FreeQuantity}", request.OrderId, request.VoucherId, newVoucher.ItemId, newVoucher.FreeQuantity);
                
                // Fix CRICTICAL BUG: So sánh MenuItemId thay vì OrderItemId để trích xuất item ra làm FreeItem
                var newFreeItem = order.OrderItems.FirstOrDefault(oi => oi.MenuItemId == newVoucher.ItemId && !oi.IsFreeItem);
                
                if (newFreeItem != null)
                {
                    _logger.LogInformation("Found existing order item for new free item in order for OrderId: {OrderId}, FreeItemOrderItemId: {FreeItemOrderItemId}", request.OrderId, newFreeItem.OrderItemId);
                    if (newFreeItem.Quantity > newVoucher.FreeQuantity)
                    {
                        _logger.LogInformation("Existing order item quantity is greater than free quantity for new free item in order for OrderId: {OrderId}, ExistingQuantity: {ExistingQuantity}, FreeQuantity: {FreeQuantity}", request.OrderId, newFreeItem.Quantity, newVoucher.FreeQuantity);
                        
                        // Tạo 1 clone order item phục vụ cho phần dư của free item
                        var duplicateNewFreeItem = newFreeItem
                            .CloneForOrder(order.OrderId, (int)(newFreeItem.Quantity - newVoucher.FreeQuantity), DateTime.UtcNow);

                        // Điều chỉnh lại quantity của order item
                        newFreeItem.AdjustQuantity((int)newVoucher.FreeQuantity);

                        await orderItemRepository.AddAsync(duplicateNewFreeItem);
                    }

                    // Đánh dấu order item là free item
                    newFreeItem.IsFreeItem = true;
                    orderItemRepository.Update(newFreeItem);
                }
            }

            _logger.LogInformation("Applying new voucher to order for OrderId: {OrderId}, VoucherId: {VoucherId}", request.OrderId, request.VoucherId);
            order.ApplyVoucher(newVoucher, auditorId);
            newVoucher.Used(auditorId);
            oldVoucher?.UnUsed(auditorId);

            orderRepository.Update(order);
            voucherRepository.Update(newVoucher);
            if (oldVoucher != null)
            {
                _logger.LogInformation("Unapplying old voucher for order for OrderId: {OrderId}, OldVoucherId: {OldVoucherId}", request.OrderId, oldVoucher.VoucherId);
                voucherRepository.Update(oldVoucher);
            }
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new ApplyVoucherResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                OldVoucherId = oldVoucher?.VoucherId,
                OldVoucherCode = oldVoucher?.VoucherCode,
                NewVoucherId = newVoucher.VoucherId,
                NewVoucherCode = newVoucher.VoucherCode,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount
            };

            return Result<ApplyVoucherResponse>.Success(response);
        }
    }
}

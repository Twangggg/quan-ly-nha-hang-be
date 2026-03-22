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
            _logger.LogInformation("Handling ApplyVoucherCommand for OrderId: {OrderId}, VoucherId: {VoucherId}", request.OrderId, request.VoucherId);

            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();

            var orderRepository = _unitOfWork.Repository<Order>();
            var voucherRepository = _unitOfWork.Repository<Voucher>();

            var order = await orderRepository
                .Query()
                .Include(o => o.Voucher)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OptionGroups)
                .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
            if (order == null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound);
                return Result<ApplyVoucherResponse>.NotFound(errorMessage);
            }

            if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.Merged) // Có thể không cần check điều kiện
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.InvalidStatus);
                return Result<ApplyVoucherResponse>.Failure(errorMessage);
            }

            var oldVoucher = order.Voucher;
            if (oldVoucher != null)
            {
                order.ApplyVoucher(null, auditorId);   // Gỡ voucher cũ khỏi order trước, tránh trường hợp gỡ voucher cũ rồi nhưng áp voucher mới không thành công thì order lại bị mất voucher cũ
            }

            var newVoucher = await voucherRepository
                .Query()
                .Include(v => v.Item)
                .FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId, cancellationToken);
            if (newVoucher == null)    // Suy nghĩ: Voucher không tồn tại thì tính là unapply cho order, gộp 2 tính năng lại, thay vì phải có 1 API riêng để unapply voucher. Nếu voucher không tồn tại thì coi như là unapply, xóa voucherId khỏi order luôn. Còn nếu voucher tồn tại nhưng không hợp lệ thì mới trả lỗi.
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFound);
                return Result<ApplyVoucherResponse>.NotFound(errorMessage);
            }

            if (!newVoucher.IsValid())
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.Invalid);
                return Result<ApplyVoucherResponse>.Failure(errorMessage);
            }

            if (order.VoucherId != null && order.VoucherId == request.VoucherId)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.VoucherAlreadyApplied);
                return Result<ApplyVoucherResponse>.Failure(errorMessage);
            }

            if (!newVoucher.IsSuitableForOrder(order))
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotSuitableForOrder);
                return Result<ApplyVoucherResponse>.Failure(errorMessage);
            }

            order.ApplyVoucher(newVoucher, auditorId);
            newVoucher.Used(auditorId);
            oldVoucher?.UnUsed(auditorId);

            orderRepository.Update(order);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new ApplyVoucherResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                VoucherId = newVoucher.VoucherId,
                VoucherCode = newVoucher.VoucherCode,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount
            };

            return Result<ApplyVoucherResponse>.Success(response);
        }
    }
}

using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Commands.CreateQrPayment
{
    public class CreateQrPaymentHandler : IRequestHandler<CreateQrPaymentCommand, Result<PaymentLinkResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<CreateQrPaymentHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public CreateQrPaymentHandler(
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            ILogger<CreateQrPaymentHandler> logger,
            IMessageService messageService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _logger = logger;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaymentLinkResponse>> Handle(CreateQrPaymentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating QR Payment for OrderId: {OrderId}", request.OrderId);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<PaymentLinkResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<PaymentLinkResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            {
                return Result<PaymentLinkResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                    ResultErrorType.BadRequest
                );
            }

            try
            {
                // Recalculate total to ensure VAT is included
                order.RecalculateTotalAmount();

                // Tính số tiền còn lại cần thanh toán (sau khi đã trả tiền mặt một phần)
                var remainingAmount = order.GetRemainingAmount();

                if (remainingAmount <= 0)
                {
                    _logger.LogWarning("Order {OrderId} already fully paid. AmountPaid: {AmountPaid}", 
                        request.OrderId, order.AmountPaid);
                    return Result<PaymentLinkResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                        ResultErrorType.BadRequest
                    );
                }

                _logger.LogInformation(
                    "Generating QR for remaining amount: {RemainingAmount} (Total: {Total}, Already Paid: {Paid})",
                    remainingAmount, order.TotalAmount, order.AmountPaid ?? 0);

                // Regenerate a unique TransactionCode to avoid "Đơn thanh toán đã tồn tại" error
                order.TransactionCode = int.Parse(DateTimeOffset.Now.ToString("HHmmssfff"));
                _unitOfWork.Repository<Order>().Update(order);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Tạo payment link với số tiền còn lại thay vì toàn bộ
                var paymentLink = await _paymentService.CreatePaymentLinkAsync(order, remainingAmount, cancellationToken);
                _logger.LogInformation("Payment link successfully generated for OrderId: {OrderId}, Amount: {Amount}",
                    request.OrderId, remainingAmount);
                return Result<PaymentLinkResponse>.Success(paymentLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment link for order {OrderId}", request.OrderId);
                return Result<PaymentLinkResponse>.Failure(
                    "Cannot create PayOS payment link.",
                    ResultErrorType.BadRequest
                );
            }
        }
    }
}

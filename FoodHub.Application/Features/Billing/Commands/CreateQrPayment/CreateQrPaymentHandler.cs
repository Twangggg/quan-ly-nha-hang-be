using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
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
                var paymentLink = await _paymentService.CreatePaymentLinkAsync(order, cancellationToken);
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

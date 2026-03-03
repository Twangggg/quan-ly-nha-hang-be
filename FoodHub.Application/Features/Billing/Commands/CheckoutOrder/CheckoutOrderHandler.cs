using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CheckoutOrderHandler> _logger;
        private readonly IMessageService _messageService;

        public CheckoutOrderHandler(IUnitOfWork unitOfWork, ILogger<CheckoutOrderHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing checkout for OrderId: {OrderId}", request.OrderId);

            var order = await _unitOfWork.Repository<Domain.Entities.Order>().GetByIdAsync(request.OrderId);
            
            if (order == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound, new { Id = request.OrderId }),
                    ResultErrorType.NotFound
                );
            }

            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                var actionStatus = order.Status == OrderStatus.Paid ? MessageKeys.Order.AlreadyPaid : MessageKeys.Order.InvalidStatusForCancel; // Using existing keys
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus, new { Status = order.Status.ToString() }),
                    ResultErrorType.BadRequest
                );
            }

            // Must be Serving
            if (order.Status != OrderStatus.Serving)
            {
                 return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus, new { Status = order.Status.ToString() }),
                    ResultErrorType.BadRequest
                );
            }

            // TotalAmount should be positive but handled just in case
            if (request.PaymentMethod == PaymentMethod.Cash)
            {
                if ((request.AmountPaid ?? 0) < order.TotalAmount)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InsufficientAmount),
                        ResultErrorType.BadRequest
                    );
                }
                order.AmountPaid = request.AmountPaid;
            }
            else if (request.PaymentMethod == PaymentMethod.QRCode)
            {
                // Mock QR: We assume full payment for Mock
                order.AmountPaid = order.TotalAmount;
            }

            // Update Order
            order.Status = OrderStatus.Paid;
            order.PaymentMethod = request.PaymentMethod;
            order.PaidAt = DateTime.UtcNow;

            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            // Update Table to Cleaning if DineIn
            if (order.OrderType == OrderType.DineIn && order.TableId.HasValue)
            {
                var table = await _unitOfWork.Repository<Domain.Entities.Table>().GetByIdAsync(order.TableId.Value);
                if (table != null)
                {
                    table.Status = TableStatus.Cleaning;
                    _unitOfWork.Repository<Domain.Entities.Table>().Update(table);
                }
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Successfully completed checkout for OrderId: {OrderId}", request.OrderId);

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

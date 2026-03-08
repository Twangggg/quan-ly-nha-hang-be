using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CheckoutOrderHandler> _logger;
        private readonly IMessageService _messageService;

        public CheckoutOrderHandler(
            IUnitOfWork unitOfWork,
            ILogger<CheckoutOrderHandler> logger,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(
            CheckoutOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Processing checkout for OrderId: {OrderId}", request.OrderId);

            var order = await _unitOfWork
                .Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(
                        MessageKeys.Order.NotFound,
                        new { Id = request.OrderId }
                    ),
                    ResultErrorType.NotFound
                );
            }

            var domainResult = order.ProcessCheckout(request.PaymentMethod, request.AmountPaid);
            if (!domainResult.IsSuccess)
            {
                if (domainResult.ErrorCode == DomainErrors.Order.InvalidActionWithStatus)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(
                            MessageKeys.Order.InvalidActionWithStatus,
                            new { Status = order.Status.ToString() }
                        ),
                        ResultErrorType.BadRequest
                    );
                }
                if (domainResult.ErrorCode == DomainErrors.Order.InsufficientAmount)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InsufficientAmount),
                        ResultErrorType.BadRequest
                    );
                }
                if (domainResult.ErrorCode == DomainErrors.Order.ItemsNotFinished)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.ItemsNotFinished),
                        ResultErrorType.BadRequest
                    );
                }
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

                // Update Table to Cleaning if DineIn
                if (order.OrderType == OrderType.DineIn && order.TableId.HasValue)
                {
                    var table = await _unitOfWork
                        .Repository<Domain.Entities.Table>()
                        .GetByIdAsync(order.TableId.Value);
                    if (table != null)
                    {
                        table.MarkAsCleaning();
                        _unitOfWork.Repository<Domain.Entities.Table>().Update(table);
                    }
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Transaction failed while checking out OrderId: {OrderId}",
                    request.OrderId
                );
                throw;
            }

            _logger.LogInformation(
                "Successfully completed checkout for OrderId: {OrderId}",
                request.OrderId
            );

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

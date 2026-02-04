using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<CancelOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);
            }

            var orderRepository = _unitOfWork.Repository<Domain.Entities.Order>();
            var order = await orderRepository.GetByIdAsync(request.OrderId);

            if (order == null)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound), ResultErrorType.NotFound);
            }

            // Only allow cancellation for Draft and Preparing status
            if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.Preparing)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Order.InvalidAction));
            }

            // Requirement 5.5: Reason is required for non-draft cancellation
            if (order.Status != OrderStatus.Draft && string.IsNullOrWhiteSpace(request.Reason))
            {
                return Result<bool>.Failure("Reason is required when cancelling an order in this status.");
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            orderRepository.Update(order);

            // Audit Log
            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = "CANCEL",
                CreatedAt = DateTime.UtcNow,
                ChangeReason = request.Reason,
                NewValue = "{\"status\": \"Cancelled\"}"
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

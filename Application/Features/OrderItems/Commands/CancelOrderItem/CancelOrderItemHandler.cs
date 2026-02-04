using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem
{
    public class CancelOrderItemHandler : IRequestHandler<CancelOrderItemCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CancelOrderItemHandler> _logger;

        public CancelOrderItemHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<CancelOrderItemHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(CancelOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);
            }

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();
            var orderItem = await orderItemRepository
                .Query()
                .FirstOrDefaultAsync(oi => oi.OrderItemId == request.OrderItemId);

            if (orderItem == null)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound), ResultErrorType.NotFound);
            }

            orderItem.Status = OrderItemStatus.Cancelled;
            orderItem.UpdatedAt = DateTime.UtcNow;
            orderItem.CanceledAt = DateTime.UtcNow;

            orderItemRepository.Update(orderItem);

            // Recalculate Order Total
            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderItem.OrderId, cancellationToken);

            if (order != null)
            {
                order.TotalAmount = order.OrderItems
                    .Where(x => x.Status != OrderItemStatus.Cancelled && x.Status != OrderItemStatus.Rejected)
                    .Sum(x => x.Quantity * x.UnitPriceSnapshot);
                order.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Domain.Entities.Order>().Update(order);
            }

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = orderItem.OrderId,
                EmployeeId = auditorId,
                Action = "CANCEL_ITEM",
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

using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<CancelOrderCommand> _logger;

        public CancelOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<CancelOrderCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized);
            }

            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.Status == OrderStatus.Serving);

            if (order == null)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound);
            }

            // Cancel order -> Cancel all items not completed/rejected
            foreach (var item in order.OrderItems)
            {
                if (item.Status == OrderItemStatus.Preparing || item.Status == OrderItemStatus.Cooking || item.Status == OrderItemStatus.Ready)
                {
                    item.Status = OrderItemStatus.Cancelled;
                    item.CanceledAt = DateTime.UtcNow;
                    item.UpdatedAt = DateTime.UtcNow;
                }
            }


            order.Status = OrderStatus.Cancelled;

            if (order.OrderType == OrderType.DineIn)
            {
                order.TableId = null;
            }

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = AuditLogActions.CancelOrder,
                CreatedAt = DateTime.UtcNow,
                ChangeReason = request.Reason,
                NewValue = "{\"status\": \"Cancelled\"}"
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating order items for OrderId {OrderId}", request.OrderId);
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }

            return Result<bool>.Success(true);
        }
    }
}

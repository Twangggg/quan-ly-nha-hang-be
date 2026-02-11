using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Orders.Commands.CompleteOrder
{
    public class CompleteOrderHandler : IRequestHandler<CompleteOrderCommand, Result<CompleteOrderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CompleteOrderCommand> _logger;
        public CompleteOrderHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ICurrentUserService currentUserService,
            ILogger<CompleteOrderCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<CompleteOrderResponse>> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<CompleteOrderResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized);
            }

            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.Status != Domain.Enums.OrderStatus.Completed);

            if (order == null)
            {
                return Result<CompleteOrderResponse>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound);
            }

            var finalAmount = order.OrderItems
                .Where(oi => oi.Status != OrderItemStatus.Cancelled && oi.Status != OrderItemStatus.Rejected)
                .Sum(item =>
                {
                    var itemTotal = item.Quantity * item.UnitPriceSnapshot;
                    var optionsTotal = item.OptionGroups?
                        .SelectMany(og => og.OptionValues)
                        .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity) ?? 0;
                    return itemTotal + (optionsTotal * item.Quantity);
                });

            order.Status = OrderStatus.Completed;
            order.TotalAmount = finalAmount;
            order.CompletedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            if (order.OrderType == OrderType.DineIn)
            {
                bool allItemsFinished = order.OrderItems.All(oi =>
                    oi.Status == OrderItemStatus.Completed ||
                    oi.Status == OrderItemStatus.Cancelled ||
                    oi.Status == OrderItemStatus.Rejected);
                if (allItemsFinished)
                {
                    // N?u ph?c v? xong h?t r?i m?i thanh toán -> Gi?i phóng bàn ngay
                    order.TableId = null;
                }
                else
                {
                    // N?u v?n còn món dang n?u/chua xong -> Gi? nguyên TableId 
                    // Bàn s? du?c gi?i phóng t? d?ng khi món cu?i cùng du?c chuy?n sang Completed ? lu?ng ph?c v?.
                }
            }

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = AuditLogActions.CompleteOrder,
                CreatedAt = DateTime.UtcNow,
                NewValue = $"{{\"finalAmount\": {finalAmount}, \"itemsCount\": {order.OrderItems.Count}}}"
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while completing OrderId {OrderId}", request.OrderId);
                return Result<CompleteOrderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }

            var response = _mapper.Map<CompleteOrderResponse>(order);
            return Result<CompleteOrderResponse>.Success(response);
        }
    }
}

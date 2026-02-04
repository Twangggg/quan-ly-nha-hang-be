using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemHandler : IRequestHandler<UpdateOrderItemCommand, Result<UpdateOrderItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateOrderItemCommand> _logger;


        public UpdateOrderItemHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<UpdateOrderItemCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<UpdateOrderItemResponse>> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn), ResultErrorType.Unauthorized);
            }

            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound), ResultErrorType.NotFound);
            }

            var incomingItems = request.Items ?? new List<UpdateOrderItemDto>();

            var itemsToRemove = order.OrderItems
                .Where(oi => !incomingItems.Any(ii => ii.OrderItemId == oi.OrderItemId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                order.OrderItems.Remove(item);
                _unitOfWork.Repository<OrderItem>().Delete(item);
            }

            foreach (var incomingItem in incomingItems)
            {
                var existingItem = order.OrderItems.FirstOrDefault(i => i.OrderItemId == incomingItem.OrderItemId);

                if (existingItem != null)
                {
                    // Update existing
                    existingItem.Quantity = incomingItem.Quantity;
                    existingItem.ItemNote = incomingItem.ItemNote;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Add new item
                    var menuItem = await _unitOfWork.Repository<MenuItem>().GetByIdAsync(incomingItem.MenuItemId);
                    if (menuItem == null)
                    {
                        return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound));
                    }

                    var price = order.OrderType == Domain.Enums.OrderType.Takeaway
                        ? menuItem.PriceTakeAway
                        : menuItem.PriceDineIn;

                    var newItem = new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        MenuItemId = incomingItem.MenuItemId,
                        Quantity = incomingItem.Quantity,
                        ItemNote = incomingItem.ItemNote,
                        CreatedAt = DateTime.UtcNow,
                        ItemNameSnapshot = menuItem.Name,
                        ItemCodeSnapshot = menuItem.Code,
                        UnitPriceSnapshot = price,
                        StationSnapshot = menuItem.Station.ToString()
                    };
                    order.OrderItems.Add(newItem);
                }
            }

            order.TotalAmount = order.OrderItems
                .Where(x => x.Status != OrderItemStatus.Cancelled && x.Status != OrderItemStatus.Rejected)
                .Sum(x => x.Quantity * x.UnitPriceSnapshot);
            order.UpdatedAt = DateTime.UtcNow;

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = "UPDATE_ITEMS",
                CreatedAt = DateTime.UtcNow,
                ChangeReason = request.Reason,
                NewValue = "{\"action\": \"Updated Order Items Sync\"}"
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
                return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }

            var response = _mapper.Map<UpdateOrderItemResponse>(order);
            return Result<UpdateOrderItemResponse>.Success(response);
        }
    }
}

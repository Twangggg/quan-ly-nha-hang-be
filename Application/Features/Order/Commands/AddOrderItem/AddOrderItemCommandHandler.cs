using FoodHub.Application.Common.Models;
using FoodHub.Domain.Entities;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Order.Commands.AddOrderItem
{
    public class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, Result<Guid>>
    {
        private readonly Interfaces.IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public AddOrderItemCommandHandler(Interfaces.IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.InvalidQuantiry));
            }
            if (order.Status != Domain.Enums.OrderStatus.Draft)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.InvalidAction));
            }

            // Get MenuItem
            var menuItem = await _unitOfWork.Repository<MenuItem>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound));
            }

            var existingItem = order.OrderItems.FirstOrDefault(x =>
                x.MenuItemId == request.MenuItemId &&
                (x.ItemNote ?? "") == (request.Note ?? "")
                );

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                // existingItem.UnitPriceSnapshot = menuItem.PriceDineIn; 
            }
            else
            {
                var price = order.OrderType == Domain.Enums.OrderType.Takeaway 
                            ? menuItem.PriceTakeAway 
                            : menuItem.PriceDineIn;

                var newItem = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    MenuItemId = request.MenuItemId,
                    Quantity = request.Quantity,
                    ItemNote = request.Note,
                    CreatedAt = DateTime.UtcNow,

                    // Snapshot from MenuItem
                    ItemNameSnapshot = menuItem.Name,
                    ItemCodeSnapshot = menuItem.Code,
                    UnitPriceSnapshot = price,
                    StationSnapshot = menuItem.Station.ToString() // Storing as string or use Enum if Snapshot is string
                };
                order.OrderItems.Add(newItem);
            }

            // Recalculate Total
            order.TotalAmount = order.OrderItems.Sum(x => x.Quantity * x.UnitPriceSnapshot);

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result<Guid>.Success(order.OrderId);
        }
    }
}

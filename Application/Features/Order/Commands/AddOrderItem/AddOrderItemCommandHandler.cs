using FoodHub.Application.Common.Models;
using FoodHub.Domain.Entities;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Order.Commands.AddOrderItem
{
    public class AddOrderItemCommadHadler : IRequestHandler<AddOrderItemCommand, Result<Guid>>
    { 
        private readonly Interfaces.IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public AddOrderItemCommadHadler(Interfaces.IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result<Guid>.Failure("Order not found");
            }
            if (order.Status != Domain.Enums.OrderStatus.Draft)
            {
                return Result<Guid>.Failure($"Cannot change order with status {order.Status}");
            }

            var exitingItem = order.OrderItems.FirstOrDefault(x =>
                x.MenuItemId == request.MenuItemId &&
                (x.ItemNote ?? "") == (request.Note ?? "")
                );

            if (exitingItem != null) 
            {
                exitingItem.Quantity += request.Quantity;
            } else
            {
                var newItem = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    MenuItemId = request.MenuItemId,
                    Quantity = request.Quantity,
                    ItemNote = request.Note,
                    CreatedAt = DateTime.UtcNow,
                    //Thieu
                };
                order.OrderItems.Add(newItem);  
            }

            order.TotalAmount = order.OrderItems.Sum(x => x.Quantity * x.UnitPriceSnapshot);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result<Guid>.Success(order.OrderId);
        }
    }
}

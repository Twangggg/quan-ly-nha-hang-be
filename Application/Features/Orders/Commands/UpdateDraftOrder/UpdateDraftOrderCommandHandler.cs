using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Orders.Commands.UpdateDraftOrder
{
    public class UpdateDraftOrderCommandHandler : IRequestHandler<UpdateDraftOrderCommand, Result<UpdateDraftOrderCommandResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateDraftOrderCommand> _logger;


        public UpdateDraftOrderCommandHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<UpdateDraftOrderCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<UpdateDraftOrderCommandResponse>> Handle(UpdateDraftOrderCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<UpdateDraftOrderCommandResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);
            }

            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<UpdateDraftOrderCommandResponse>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound), ResultErrorType.NotFound);
            }

            if (order.Status != Domain.Enums.OrderStatus.Draft)
            {
                return Result<UpdateDraftOrderCommandResponse>.Failure(_messageService.GetMessage(MessageKeys.Order.InvalidAction));
            }

            order.Note = request.Note;
            order.OrderType = request.OrderType;
            order.TableId = request.TableId;
            order.UpdatedAt = DateTime.UtcNow;

            var currentItems = order.OrderItems.ToList();
            var incomingItems = request.OrderItems ?? new List<UpdateOrderItemDto>();

            foreach (var currentItem in currentItems)
            {
                if (!incomingItems.Any(i => i.OrderItemId == currentItem.OrderItemId))
                {
                    order.OrderItems.Remove(currentItem);
                }
            }

            foreach (var incomingItem in incomingItems)
            {
                var existingItem = order.OrderItems.FirstOrDefault(i => i.OrderItemId == incomingItem.OrderItemId);

                if (existingItem != null)
                {
                    // Update existing
                    existingItem.Quantity = incomingItem.Quantity;
                    existingItem.ItemNote = incomingItem.Note;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var menuItem = await _unitOfWork.Repository<MenuItem>().GetByIdAsync(incomingItem.MenuItemId);
                    if (menuItem != null)
                    {
                        var price = order.OrderType == Domain.Enums.OrderType.Takeaway
                            ? menuItem.PriceTakeAway
                            : menuItem.PriceDineIn;

                        order.OrderItems.Add(new Domain.Entities.OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            MenuItemId = incomingItem.MenuItemId,
                            Quantity = incomingItem.Quantity,
                            ItemNote = incomingItem.Note,
                            CreatedAt = DateTime.UtcNow,
                            ItemNameSnapshot = menuItem.Name,
                            ItemCodeSnapshot = menuItem.Code,
                            UnitPriceSnapshot = price,
                            StationSnapshot = menuItem.Station.ToString()
                        });
                    }
                }
            }

            order.TotalAmount = order.OrderItems.Sum(x => x.Quantity * x.UnitPriceSnapshot);

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = "UPDATE",
                CreatedAt = DateTime.UtcNow,
                NewValue = "{\"action\": \"Updated Order Details and Items\"}"
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating order {OrderId}", request.OrderId);
                return Result<UpdateDraftOrderCommandResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }

            var response = _mapper.Map<UpdateDraftOrderCommandResponse>(order);
            return Result<UpdateDraftOrderCommandResponse>.Success(response);
        }
    }
}

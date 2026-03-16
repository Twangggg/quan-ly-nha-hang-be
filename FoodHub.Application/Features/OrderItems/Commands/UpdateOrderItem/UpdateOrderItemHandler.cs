using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Features.OrderItems.Commands.AddOrderItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemHandler
        : IRequestHandler<UpdateOrderItemCommand, Result<UpdateOrderItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateOrderItemHandler> _logger;

        public UpdateOrderItemHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<UpdateOrderItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<UpdateOrderItemResponse>> Handle(
            UpdateOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                _logger.LogWarning(
                    "Unauthorized user attempt to update order {OrderId}.",
                    request.OrderId
                );
                return Result<UpdateOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<UpdateOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // BR: Không cho phép chỉnh sửa order đã hoàn thành hoặc hủy
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                _logger.LogWarning(
                    "Cannot update items for Order {OrderId} because status is {Status}.",
                    order.OrderId,
                    order.Status
                );
                return Result<UpdateOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );
            }

            var incomingItems = request.Items ?? new List<UpdateOrderItemDto>();

            var itemsToRemove = order
                .OrderItems.Where(oi =>
                    oi.Status != OrderItemStatus.Cancelled
                    && !incomingItems.Any(ii => ii.OrderItemId == oi.OrderItemId)
                )
                .ToList();

            // BR: Items đang được nấu, hoàn thành không thể xóa (KDS-05)
            if (itemsToRemove.Any(oi => oi.Status != OrderItemStatus.Preparing))
            {
                _logger.LogWarning(
                    "Attempted to cancel order items not in Preparing state for Order {OrderId}.",
                    order.OrderId
                );
                return Result<UpdateOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var item in itemsToRemove)
                {
                    var cancelResult = item.Cancel();
                    if (!cancelResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<UpdateOrderItemResponse>.Failure(
                            _messageService.GetMessage(
                                cancelResult.ErrorCode ?? MessageKeys.Order.InvalidActionWithStatus
                            ),
                            ResultErrorType.BadRequest
                        );
                    }
                    item.UpdatedAt = DateTime.UtcNow;
                }

                foreach (var incomingItem in incomingItems)
                {
                    var existingItem = order.OrderItems.FirstOrDefault(i =>
                        i.OrderItemId == incomingItem.OrderItemId
                    );

                    if (existingItem != null)
                    {
                        var updatedOptionGroups = await BuildOptionGroupsAsync(
                            existingItem.OrderItemId,
                            incomingItem.SelectedOptions,
                            cancellationToken
                        );

                        var updateResult = existingItem.UpdateDetails(
                            incomingItem.Quantity,
                            incomingItem.ItemNote,
                            updatedOptionGroups
                        );
                        if (!updateResult.IsSuccess)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            return Result<UpdateOrderItemResponse>.Failure(
                                _messageService.GetMessage(
                                    updateResult.ErrorCode ?? MessageKeys.Order.InvalidActionWithStatus
                                ),
                                ResultErrorType.BadRequest
                            );
                        }
                    }
                    else
                    {
                        // Add new item
                        var menuItem = await _unitOfWork
                            .Repository<MenuItem>()
                            .GetByIdAsync(incomingItem.MenuItemId);
                        if (menuItem == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            return Result<UpdateOrderItemResponse>.Failure(
                                _messageService.GetMessage(MessageKeys.MenuItem.NotFound)
                            );
                        }

                        var price = menuItem.Price;

                        var newItem = new OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            OrderId = order.OrderId,
                            MenuItemId = incomingItem.MenuItemId,
                            Quantity = incomingItem.Quantity,
                            ItemNote = incomingItem.ItemNote,
                            CreatedAt = DateTime.UtcNow,
                            Status = OrderItemStatus.Preparing,
                            ItemNameSnapshot = menuItem.Name,
                            ItemCodeSnapshot = menuItem.Code,
                            UnitPriceSnapshot = price,
                            StationSnapshot = menuItem.Station.ToString(),
                        };

                        var optionGroups = await BuildOptionGroupsAsync(
                            newItem.OrderItemId,
                            incomingItem.SelectedOptions,
                            cancellationToken
                        );
                        foreach (var optionGroup in optionGroups)
                        {
                            newItem.OptionGroups.Add(optionGroup);
                        }
                        order.OrderItems.Add(newItem);
                    }
                }

                order.RecalculateTotalAmount();
                order.UpdatedAt = DateTime.UtcNow;

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    EmployeeId = auditorId.Value,
                    Action = AuditLogActions.UpdateOrderItem,
                    CreatedAt = DateTime.UtcNow,
                    ChangeReason = request.Reason,
                    NewValue = "{\"action\": \"Updated Order Items Sync\"}",
                };

                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
                _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully updated order items for Order {OrderId}.",
                    order.OrderId
                );

                var response = _mapper.Map<UpdateOrderItemResponse>(order);
                return Result<UpdateOrderItemResponse>.Success(response);
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database error occurred while updating order items for OrderId {OrderId}",
                    request.OrderId
                );
                return Result<UpdateOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Unexpected error occurred while updating order items for OrderId {OrderId}",
                    request.OrderId
                );
                throw;
            }
        }

        private async Task<List<OrderItemOptionGroup>> BuildOptionGroupsAsync(
            Guid orderItemId,
            List<OrderItemOptionGroupDto>? selectedOptions,
            CancellationToken cancellationToken
        )
        {
            if (selectedOptions == null || !selectedOptions.Any())
                return new List<OrderItemOptionGroup>();

            var optionGroupIds = selectedOptions.Select(og => og.OptionGroupId).ToList();
            var optionItemIds = selectedOptions
                .SelectMany(og => og.SelectedValues)
                .Select(v => v.OptionItemId)
                .ToList();

            var optionGroups = await _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .Where(og => optionGroupIds.Contains(og.OptionGroupId))
                .ToDictionaryAsync(og => og.OptionGroupId, cancellationToken);

            var optionItems = await _unitOfWork
                .Repository<OptionItem>()
                .Query()
                .Where(oi => optionItemIds.Contains(oi.OptionItemId))
                .ToDictionaryAsync(oi => oi.OptionItemId, cancellationToken);

            var builtOptionGroups = new List<OrderItemOptionGroup>();
            foreach (var optionGroupDto in selectedOptions)
            {
                if (optionGroups.TryGetValue(optionGroupDto.OptionGroupId, out var ogDef))
                {
                    var orderItemOptionGroup = new OrderItemOptionGroup
                    {
                        OrderItemOptionGroupId = Guid.NewGuid(),
                        OrderItemId = orderItemId,
                        GroupNameSnapshot = ogDef.Name,
                        GroupTypeSnapshot = ogDef.OptionType.ToString(),
                        IsRequiredSnapshot = ogDef.IsRequired,
                        CreatedAt = DateTime.UtcNow,
                    };

                    foreach (var valueDto in optionGroupDto.SelectedValues)
                    {
                        if (optionItems.TryGetValue(valueDto.OptionItemId, out var oiDef))
                        {
                            var orderItemOptionValue = new OrderItemOptionValue
                            {
                                OrderItemOptionValueId = Guid.NewGuid(),
                                OrderItemOptionGroupId =
                                    orderItemOptionGroup.OrderItemOptionGroupId,
                                OptionItemId = valueDto.OptionItemId,
                                LabelSnapshot = oiDef.Label,
                                ExtraPriceSnapshot = oiDef.ExtraPrice,
                                Quantity = valueDto.Quantity,
                                Note = valueDto.Note,
                                CreatedAt = DateTime.UtcNow,
                            };
                            orderItemOptionGroup.OptionValues.Add(orderItemOptionValue);
                        }
                    }
                    builtOptionGroups.Add(orderItemOptionGroup);
                }
            }

            return builtOptionGroups;
        }
    }
}

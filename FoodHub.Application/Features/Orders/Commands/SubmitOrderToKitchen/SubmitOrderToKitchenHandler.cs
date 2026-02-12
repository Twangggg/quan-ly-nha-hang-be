using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen
{
    public class SubmitOrderToKitchenHandler
        : IRequestHandler<SubmitOrderToKitchenCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public SubmitOrderToKitchenHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(
            SubmitOrderToKitchenCommand request,
            CancellationToken cancellationToken
        )
        {
            //Get current user
            var currentIdString = _currentUserService.UserId;
            if (
                string.IsNullOrEmpty(currentIdString)
                || !Guid.TryParse(currentIdString, out var userId)
            )
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            //Validate Table for dine in
            if (request.OrderType == OrderType.DineIn)
            {
                if (!request.TableId.HasValue)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.SelectTable)
                    );
                }

                var existingServingOrder = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .AnyAsync(
                        o => o.TableId == request.TableId && o.Status == OrderStatus.Serving,
                        cancellationToken
                    );

                if (existingServingOrder)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.TableAlreadyOccupied)
                    );
                }
            }

            //Validate All Menu Items Exist
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var menuItems = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .Where(m => menuItemIds.Contains(m.MenuItemId))
                .ToDictionaryAsync(m => m.MenuItemId, cancellationToken);
            if (menuItems.Count != menuItemIds.Count)
            {
                var missingIds = menuItemIds.Except(menuItems.Keys);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.NotFound)
                );
            }

            // Validate Options Exist (if provided)
            var allOptionGroupIds = request
                .Items.Where(i => i.SelectedOptions != null)
                .SelectMany(i => i.SelectedOptions!)
                .Select(og => og.OptionGroupId)
                .Distinct()
                .ToList();
            var allOptionItemIds = request
                .Items.Where(i => i.SelectedOptions != null)
                .SelectMany(i => i.SelectedOptions!)
                .SelectMany(og => og.SelectedValues)
                .Select(v => v.OptionItemId)
                .Distinct()
                .ToList();
            Dictionary<Guid, OptionGroup> optionGroups = new();
            Dictionary<Guid, OptionItem> optionItems = new();
            if (allOptionGroupIds.Any())
            {
                optionGroups = await _unitOfWork
                    .Repository<OptionGroup>()
                    .Query()
                    .Where(og => allOptionGroupIds.Contains(og.OptionGroupId))
                    .ToDictionaryAsync(og => og.OptionGroupId, cancellationToken);
                if (optionGroups.Count != allOptionGroupIds.Count)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.OptionGroup.NotFound)
                    );
                }
            }
            if (allOptionItemIds.Any())
            {
                optionItems = await _unitOfWork
                    .Repository<OptionItem>()
                    .Query()
                    .Where(oi => allOptionItemIds.Contains(oi.OptionItemId))
                    .ToDictionaryAsync(oi => oi.OptionItemId, cancellationToken);
                if (optionItems.Count != allOptionItemIds.Count)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.OptionItem.NotFound)
                    );
                }
            }

            //Check Out of Stock
            var outOfStockItem = menuItems.Values.Where(x => x.IsOutOfStock).ToList();
            if (outOfStockItem.Any())
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.OutOfStock)
                        + $"{string.Join(", ", outOfStockItem.Select(m => m.Name))}"
                );
            }

            //Gen Order code
            var orderCode = await GenerateOrderCodeAsync(cancellationToken);

            //Create Order entity
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = orderCode,
                OrderType = request.OrderType,
                Status = OrderStatus.Serving,
                TableId = request.OrderType == OrderType.DineIn ? request.TableId : null,
                Note = request.Note,
                TotalAmount = 0, //calculated after items
                IsPriority = false,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                OrderItems = new List<OrderItem>(),
                OrderAuditLogs = new List<OrderAuditLog>(),
            };

            //Merge Duplicate Items and Create Order Items
            // Group by MenuItemId + Note + Options to merge duplicates
            var groupedItems = request
                .Items.Select(i => new
                {
                    Item = i,
                    // Create option signature for grouping
                    OptionSignature = i.SelectedOptions == null
                        ? string.Empty
                        : string.Join(
                            "|",
                            i.SelectedOptions.OrderBy(og => og.OptionGroupId)
                                .Select(og =>
                                    $"{og.OptionGroupId}:{string.Join(",",
                                og.SelectedValues
                                    .OrderBy(v => v.OptionItemId)
                                    .Select(v => $"{v.OptionItemId}x{v.Quantity}"))}"
                                )
                        ),
                })
                .GroupBy(x => new
                {
                    x.Item.MenuItemId,
                    Note = x.Item.Note ?? string.Empty,
                    x.OptionSignature,
                })
                .Select(g => new
                {
                    g.Key.MenuItemId,
                    Note = string.IsNullOrEmpty(g.Key.Note) ? null : g.Key.Note,
                    TotalQuantity = g.Sum(x => x.Item.Quantity),
                    SelectedOptions = g.First().Item.SelectedOptions, // Use first item's options
                })
                .ToList();
            foreach (var group in groupedItems)
            {
                var menuItem = menuItems[group.MenuItemId];
                // Select price based on order type
                var price =
                    request.OrderType == OrderType.Takeaway
                        ? menuItem.PriceTakeAway
                        : menuItem.PriceDineIn;
                var orderItem = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    MenuItemId = group.MenuItemId,
                    Quantity = group.TotalQuantity,
                    ItemNote = group.Note,
                    Status = OrderItemStatus.Preparing,
                    CreatedAt = DateTime.UtcNow,
                    //snapshot
                    ItemCodeSnapshot = menuItem.Code,
                    ItemNameSnapshot = menuItem.Name,
                    UnitPriceSnapshot = price,
                    StationSnapshot = menuItem.Station.ToString(),
                };

                // NEW: Add option snapshots
                if (group.SelectedOptions != null && group.SelectedOptions.Any())
                {
                    foreach (var optionGroupDto in group.SelectedOptions)
                    {
                        var optionGroup = optionGroups[optionGroupDto.OptionGroupId];

                        var orderItemOptionGroup = new OrderItemOptionGroup
                        {
                            OrderItemOptionGroupId = Guid.NewGuid(),
                            OrderItemId = orderItem.OrderItemId,
                            GroupNameSnapshot = optionGroup.Name,
                            GroupTypeSnapshot = optionGroup.OptionType.ToString(),
                            IsRequiredSnapshot = optionGroup.IsRequired,
                            CreatedAt = DateTime.UtcNow,
                        };

                        foreach (var valueDto in optionGroupDto.SelectedValues)
                        {
                            var optionItem = optionItems[valueDto.OptionItemId];

                            var orderItemOptionValue = new OrderItemOptionValue
                            {
                                OrderItemOptionValueId = Guid.NewGuid(),
                                OrderItemOptionGroupId =
                                    orderItemOptionGroup.OrderItemOptionGroupId,
                                OptionItemId = valueDto.OptionItemId, // Trace back
                                LabelSnapshot = optionItem.Label,
                                ExtraPriceSnapshot = optionItem.ExtraPrice,
                                Quantity = valueDto.Quantity,
                                Note = valueDto.Note,
                                CreatedAt = DateTime.UtcNow,
                            };

                            orderItemOptionGroup.OptionValues.Add(orderItemOptionValue);
                        }

                        orderItem.OptionGroups.Add(orderItemOptionGroup);
                    }
                }

                order.OrderItems.Add(orderItem);
            }

            //Calculate total amount
            //Calculate total amount including option extra prices
            order.TotalAmount = order.OrderItems.Sum(item =>
            {
                var itemTotal = item.Quantity * item.UnitPriceSnapshot;
                var optionsTotal = item
                    .OptionGroups.SelectMany(og => og.OptionValues)
                    .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity);
                return itemTotal + (optionsTotal * item.Quantity); // Option price x item quantity
            });

            //Audit Log
            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = userId,
                Action = AuditLogActions.SubmitOrder, // ? Only one action: SUBMIT (no CREATE or ADD_ITEM)
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<Order>().AddAsync(order);
            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result<Guid>.Success(order.OrderId);
        }

        /// <summary>
        /// Generate unique order code in format: ORD-yyyyMMdd-xxxx
        /// Thread-safe sequential numbering per day
        /// </summary>
        private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var dateString = today.ToString("yyyyMMdd");
            var prefix = $"ORD-{dateString}-";
            // Get last order code for today
            var lastOrder = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);
            int sequenceNumber = 1;
            if (lastOrder != null)
            {
                var parts = lastOrder.OrderCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                {
                    sequenceNumber = lastSequence + 1;
                }
            }
            return $"{prefix}{sequenceNumber:D4}";
        }
    }
}

using System.ComponentModel;
using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    public class SplitOrderHandler : IRequestHandler<SplitOrderCommand, Result<SplitOrderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<SplitOrderHandler> _logger;
        private readonly IMapper _mapper;

        public SplitOrderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, IMapper mapper, ILogger<SplitOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<SplitOrderResponse>> Handle(SplitOrderCommand request, CancellationToken cancellationToken)
        {
            // Log the incoming request details
            var repoOrder = _unitOfWork.Repository<Order>();
            var repoOrderItem = _unitOfWork.Repository<OrderItem>();

            // Attempt to parse user ID for auditing
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            _logger.LogInformation(
                "Starting split operation: SourceOrder={SourceOrderId}, ItemsToSplit={ItemCount}, User={UserId}",
                request.SourceOrderId,
                request.ItemsToSplit.Count,
                auditorId
            );

            // Validate source order
            var sourceOrder = await repoOrder.Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.SourceOrderId, cancellationToken);
            if (sourceOrder is null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound, request.SourceOrderId);
                return Result<SplitOrderResponse>.NotFound(errorMessage);
            }
            if (sourceOrder.Status != OrderStatus.Completed)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.StatusNotCompleted, request.SourceOrderId);
                return Result<SplitOrderResponse>.Failure(errorMessage);
            }

            var itemsToFullySplit = 0;

            // Validate items to split
            foreach (var itemToSplit in request.ItemsToSplit)
            {
                var orderItem = sourceOrder.OrderItems.FirstOrDefault(
                    oi => oi.OrderItemId == itemToSplit.OrderItemId
                );

                if (orderItem is null)
                {
                    var errorMessage = _messageService.GetMessage(
                        MessageKeys.OrderItem.NotFound,
                        itemToSplit.OrderItemId
                    );
                    return Result<SplitOrderResponse>.NotFound(errorMessage);
                }

                if (itemToSplit.QuantityToSplit > orderItem.Quantity)
                {
                    var errorMessage = _messageService.GetMessage(
                        MessageKeys.OrderItem.InvalidQuantity,
                        itemToSplit.OrderItemId
                    );
                    return Result<SplitOrderResponse>.Failure(errorMessage);
                }

                if (itemToSplit.QuantityToSplit == orderItem.Quantity)
                {
                    itemsToFullySplit++;
                }
            }

            if (itemsToFullySplit == sourceOrder.OrderItems.Count)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity);
                return Result<SplitOrderResponse>.Failure(errorMessage);
            }

            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Generate new order code
                var newOrderCode = await GenerateOrderCodeAsync(cancellationToken);

                // Create new order
                var newOrder = new Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderCode = newOrderCode,
                    OrderType = sourceOrder.OrderType,
                    Status = OrderStatus.Completed,
                    TableId = sourceOrder.TableId,
                    Note = $"Split from Order {sourceOrder.OrderCode}",
                    TotalAmount = 0,
                    IsPriority = sourceOrder.IsPriority,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = auditorId,
                    CompletedAt = DateTime.UtcNow,
                };

                _logger.LogInformation("Creating new order {NewOrderCode} for table {TableId}", newOrderCode, sourceOrder.TableId);

                // Process each item to split
                foreach (var itemToSplit in request.ItemsToSplit)
                {
                    var sourceItem = sourceOrder.OrderItems.First(oi => oi.OrderItemId == itemToSplit.OrderItemId);

                    _logger.LogInformation(
                        "Splitting item {OrderItemId}: OriginalQuantity={OriginalQuantity}, QuantityToSplit={QuantityToSplit}",
                        sourceItem.OrderItemId,
                        sourceItem.Quantity,
                        itemToSplit.QuantityToSplit
                    );

                    // Create new item for new order with split quantity
                    var newItem = new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = newOrder.OrderId,
                        MenuItemId = sourceItem.MenuItemId,
                        ItemCodeSnapshot = sourceItem.ItemCodeSnapshot,
                        ItemNameSnapshot = sourceItem.ItemNameSnapshot,
                        StationSnapshot = sourceItem.StationSnapshot,
                        Status = sourceItem.Status,
                        Quantity = itemToSplit.QuantityToSplit,
                        UnitPriceSnapshot = sourceItem.UnitPriceSnapshot,
                        ItemNote = sourceItem.ItemNote,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Clone option groups and values
                    foreach (var optionGroup in sourceItem.OptionGroups)
                    {
                        var newOptionGroup = new OrderItemOptionGroup
                        {
                            OrderItemOptionGroupId = Guid.NewGuid(),
                            OrderItemId = newItem.OrderItemId,
                            GroupNameSnapshot = optionGroup.GroupNameSnapshot,
                            GroupTypeSnapshot = optionGroup.GroupTypeSnapshot,
                            IsRequiredSnapshot = optionGroup.IsRequiredSnapshot,
                            CreatedAt = DateTime.UtcNow
                        };

                        foreach (var optionValue in optionGroup.OptionValues)
                        {
                            var newOptionValue = new OrderItemOptionValue
                            {
                                OrderItemOptionValueId = Guid.NewGuid(),
                                OrderItemOptionGroupId = newOptionGroup.OrderItemOptionGroupId,
                                OptionItemId = optionValue.OptionItemId,
                                LabelSnapshot = optionValue.LabelSnapshot,
                                ExtraPriceSnapshot = optionValue.ExtraPriceSnapshot,
                                Quantity = optionValue.Quantity,
                                Note = optionValue.Note,
                                CreatedAt = DateTime.UtcNow
                            };
                            newOptionGroup.OptionValues.Add(newOptionValue);
                        }

                        newItem.OptionGroups.Add(newOptionGroup);
                    }

                    newOrder.OrderItems.Add(newItem);

                    sourceItem.UpdatedAt = DateTime.UtcNow;

                    // Update source item quantity or remove if fully split
                    if (itemToSplit.QuantityToSplit == sourceItem.Quantity)
                    {
                        repoOrderItem.Delete(sourceItem);

                        _logger.LogDebug(
                            "Fully moved item {ItemName} to new order",
                            sourceItem.ItemNameSnapshot
                        );
                    }
                    else
                    {
                        sourceItem.Quantity -= itemToSplit.QuantityToSplit;
                        repoOrderItem.Update(sourceItem);

                        _logger.LogDebug(
                            "Reduced source item {ItemName} quantity to {RemainingQty}",
                            sourceItem.ItemNameSnapshot,
                            sourceItem.Quantity
                        );
                    }
                }

                newOrder.TotalAmount = newOrder.OrderItems.Sum(oi => oi.GetTotalPrice());
                await repoOrder.AddAsync(newOrder);

                // Recalculate total amounts
                sourceOrder.TotalAmount -= newOrder.TotalAmount;
                sourceOrder.UpdatedAt = DateTime.UtcNow;
                sourceOrder.UpdatedBy = auditorId;
                repoOrder.Update(sourceOrder);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully split Order {SourceOrderCode} into {NewOrderCode}. Source Amount: {SourceAmount}, New Amount: {NewAmount}",
                    sourceOrder.OrderCode,
                    newOrder.OrderCode,
                    sourceOrder.TotalAmount,
                    newOrder.TotalAmount
                );

                // Reload order items for accurate response
                var sourceOrderItems = await repoOrderItem
                    .Query()
                    .Include(oi => oi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                    .Where(oi => oi.OrderId == sourceOrder.OrderId)
                    .ToListAsync(cancellationToken);

                var newOrderItems = await repoOrderItem
                    .Query()
                    .Include(oi => oi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                    .Where(oi => oi.OrderId == newOrder.OrderId)
                    .ToListAsync(cancellationToken);

                // Prepare response
                var response = new SplitOrderResponse
                {
                    SourceOrderId = sourceOrder.OrderId,
                    SourceOrderCode = sourceOrder.OrderCode,
                    SourceOrderTotalAmount = sourceOrder.TotalAmount,
                    SourceOrderItems = _mapper.Map<List<SplitOrderItemDto>>(sourceOrderItems),

                    NewOrderId = newOrder.OrderId,
                    NewOrderCode = newOrder.OrderCode,
                    NewOrderTotalAmount = newOrder.TotalAmount,
                    NewOrderItems = _mapper.Map<List<SplitOrderItemDto>>(newOrderItems)
                };

                return Result<SplitOrderResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to split Order {SourceOrderId}", request.SourceOrderId);
                throw;
            }
        }

        private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var dateString = today.ToString("yyyyMMdd");
            var prefix = $"ORD-{dateString}-";

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

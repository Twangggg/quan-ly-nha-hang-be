using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    public class MergeOrderHandler
        : IRequestHandler<MergeOrderCommand, Result<MergeOrderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<MergeOrderHandler> _logger;
        private readonly IMapper _mapper;

        public MergeOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<MergeOrderHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<MergeOrderResponse>> Handle(MergeOrderCommand request, CancellationToken cancellationToken)
        {
            // Log the incoming request details
            var repoOrder = _unitOfWork.Repository<Order>();
            var repoOrderItem = _unitOfWork.Repository<OrderItem>();
            var repoTable = _unitOfWork.Repository<Table>();

            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            _logger.LogInformation(
                "Starting merge operation: FirstOrder={FirstOrderId}, SecondOrder={SecondOrderId}, User={UserId}",
                request.FirstOrder,
                request.SecondOrder,
                auditorId
            );

            // Validate first order
            var firstOrder = await repoOrder.Query()
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == request.FirstOrder, cancellationToken);
            if (firstOrder is null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound, request.FirstOrder);
                return Result<MergeOrderResponse>.NotFound(errorMessage);
            }
            if (firstOrder.OrderType != OrderType.DineIn)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.InvalidType, request.FirstOrder);
                return Result<MergeOrderResponse>.Failure(errorMessage);
            }
            if (firstOrder.Status != OrderStatus.Serving)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.StatusNotServing, request.FirstOrder);
                return Result<MergeOrderResponse>.Failure(errorMessage);
            }

            // Validate second order
            var secondOrder = await repoOrder.Query()
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == request.SecondOrder, cancellationToken);

            if (secondOrder is null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound, request.SecondOrder);
                return Result<MergeOrderResponse>.NotFound(errorMessage);
            }
            if (secondOrder.OrderType != OrderType.DineIn)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.InvalidType, request.SecondOrder);
                return Result<MergeOrderResponse>.Failure(errorMessage);
            }
            if (secondOrder.Status != OrderStatus.Serving)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.StatusNotServing, request.SecondOrder);
                return Result<MergeOrderResponse>.Failure(errorMessage);
            }

            // Begin transaction for merging orders
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Load all items from both orders with their options
                var orderItems = await repoOrderItem.Query()
                .Include(oi => oi.OptionGroups)
                .ThenInclude(og => og.OptionValues)
                .Where(oi => oi.OrderId == firstOrder.OrderId || oi.OrderId == secondOrder.OrderId)
                .ToListAsync(cancellationToken);

                var firstOrderItems = orderItems.Where(foi => foi.OrderId == firstOrder.OrderId).ToList();
                var secondOrderItems = orderItems.Where(soi => soi.OrderId == secondOrder.OrderId).ToList();

                _logger.LogInformation(
                        "Merging {SecondItemCount} items from Order {SecondOrderCode} into Order {FirstOrderCode}",
                        secondOrderItems.Count,
                        secondOrder.OrderCode,
                        firstOrder.OrderCode
                    );

                // Merge items from second order into first order
                foreach (var secondItem in secondOrderItems)
                {
                    // Try to find an existing item in the first order that matches the second item (same MenuItemId, Status, ItemNote, and Options)
                    var existingItem = firstOrderItems
                        .FirstOrDefault(foi => foi.MenuItemId == secondItem.MenuItemId
                        && foi.Status == secondItem.Status
                        && (foi.ItemNote?.Equals(secondItem.ItemNote) ?? secondItem.ItemNote == null)
                        && AreOptionsEqual(foi.OptionGroups, secondItem.OptionGroups));

                    // If a matching item exists, merge quantities and notes; otherwise, move the item to the first order
                    if (existingItem != null)
                    {
                        _logger.LogDebug(
                                "Merging item {ItemName}: Quantity {OldQty} + {AddQty} = {NewQty}",
                                secondItem.ItemNameSnapshot,
                                existingItem.Quantity,
                                secondItem.Quantity,
                                existingItem.Quantity + secondItem.Quantity
                            );

                        existingItem.Quantity += secondItem.Quantity;
                        // Merge notes properly (avoid null/empty concatenation)
                        if (!string.IsNullOrEmpty(secondItem.ItemNote))
                        {
                            existingItem.ItemNote = string.IsNullOrEmpty(existingItem.ItemNote)
                                ? secondItem.ItemNote
                                : $"{existingItem.ItemNote}; {secondItem.ItemNote}";
                        }
                        existingItem.UpdatedAt = DateTime.UtcNow;
                        repoOrderItem.Update(existingItem);

                        // Delete the merged item from second order
                        repoOrderItem.Delete(secondItem);
                    }
                    else
                    {
                        _logger.LogDebug(
                                "Moving item {ItemName} (Quantity: {Qty}) to Order {OrderCode}",
                                secondItem.ItemNameSnapshot,
                                secondItem.Quantity,
                                firstOrder.OrderCode
                            );

                        // Update the OrderId to move the item to the first order
                        secondItem.OrderId = firstOrder.OrderId;
                        secondItem.UpdatedAt = DateTime.UtcNow;
                        repoOrderItem.Update(secondItem);
                    }
                }

                // Recalculate total amount for the first order after merging items
                decimal newTotalAmount = firstOrderItems.Sum(oi => oi.GetTotalPrice())
                                         + secondOrderItems.Sum(oi => oi.GetTotalPrice());

                // Assuming GetTotalPrice() calculates the total price of the item including options
                firstOrder.TotalAmount = newTotalAmount;
                firstOrder.UpdatedAt = DateTime.UtcNow;
                firstOrder.UpdatedBy = auditorId;

                // Merge notes from both orders
                if (!string.IsNullOrEmpty(secondOrder.Note))
                {
                    firstOrder.Note = string.IsNullOrEmpty(firstOrder.Note)
                        ? secondOrder.Note
                        : $"{firstOrder.Note}; {secondOrder.Note}";
                }
                repoOrder.Update(firstOrder);

                // Mark the second order as deleted (soft delete)
                secondOrder.DeletedAt = DateTime.UtcNow;
                secondOrder.UpdatedBy = auditorId;
                secondOrder.Note = $"Merged into Order {firstOrder.OrderCode}";
                repoOrder.Update(secondOrder);

                // Update table status if necessary (e.g., if the second order's table is different and needs to be freed up)
                if (secondOrder.TableId != firstOrder.TableId)
                {
                    // log the table status updates for debugging
                    _logger.LogInformation(
                        "Updating table status for Table {TableId} from Order {SecondOrderCode}",
                        secondOrder.TableId,
                        secondOrder.OrderCode
                    );

                    // Free up the second order's table if it exists
                    var secondTable = await repoTable.Query()
                        .Include(o => o.Orders)
                        .FirstOrDefaultAsync(t => t.TableId == secondOrder.TableId, cancellationToken);
                    if (secondTable != null)
                    {
                        // Exclude soft-deleted orders from availability checks
                        secondTable.Orders = secondTable.Orders
                            .Where(o => o.DeletedAt == null)
                            .ToList();

                        if (secondTable.SetAvailable())
                        {
                            secondTable.UpdatedAt = DateTime.UtcNow;
                            secondTable.UpdatedBy = auditorId;
                            repoTable.Update(secondTable);
                        }
                    }
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                        "Successfully merged Order {SecondOrderCode} into {FirstOrderCode}. New TotalAmount: {TotalAmount}",
                        secondOrder.OrderCode,
                        firstOrder.OrderCode,
                        firstOrder.TotalAmount
                    );

                var mergedOrderItems = await repoOrderItem.Query()
                    .Include(moi => moi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                    .Where(moi => moi.OrderId == firstOrder.OrderId)
                    .ToListAsync(cancellationToken);

                var mergedOrder = new MergeOrderResponse
                {
                    MergedOrderId = firstOrder.OrderId,
                    MergedOrderCode = firstOrder.OrderCode,
                    MergedOrderTotalAmount = firstOrder.TotalAmount,
                    Items = _mapper.Map<List<OrderItemDto>>(mergedOrderItems)
                };

                var response = mergedOrder;
                return Result<MergeOrderResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                            "Failed to merge Order {SecondOrderId} into {FirstOrderId}",
                            request.SecondOrder,
                            request.FirstOrder
                        );
                throw;
            }
        }
        private bool AreOptionsEqual(
            ICollection<OrderItemOptionGroup> options1,
            ICollection<OrderItemOptionGroup> options2)
        {
            // Quick check: if the counts of option groups are different, they can't be equal
            if (options1.Count != options2.Count)
                return false;

            // For each option group in the first item, try to find a matching group in the second item
            foreach (var og1 in options1)
            {
                // Find a matching option group in the second item based on GroupNameSnapshot
                var og2 = options2.FirstOrDefault(og =>
                    og.GroupNameSnapshot.Equals(og1.GroupNameSnapshot)
                );
                if (og2 == null)
                    return false;

                // If a matching group is found, compare their option values
                if (og1.OptionValues.Count != og2.OptionValues.Count)
                    return false;

                // For each option value in the first group, try to find a matching value in the second group
                foreach (var ov1 in og1.OptionValues)
                {
                    var ov2 = og2.OptionValues.FirstOrDefault(ov =>
                        ov.LabelSnapshot == ov1.LabelSnapshot
                        && ov.ExtraPriceSnapshot == ov1.ExtraPriceSnapshot
                        && ov.Quantity == ov1.Quantity
                    );
                    if (ov2 == null)
                        return false;
                }
            }

            return true;
        }
    }
}

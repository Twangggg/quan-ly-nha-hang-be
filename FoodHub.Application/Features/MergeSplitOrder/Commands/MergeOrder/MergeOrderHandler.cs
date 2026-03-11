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
        private readonly IMapper _mapper;
        private readonly ILogger<MergeOrderHandler> _logger;

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
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<MergeOrderResponse>> Handle(MergeOrderCommand request, CancellationToken cancellationToken)
        {
            // Log the incoming request details
            var repoOrder = _unitOfWork.Repository<Order>();
            var repoOrderItem = _unitOfWork.Repository<OrderItem>();

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
                        existingItem.ItemNote = string.IsNullOrEmpty(existingItem.ItemNote)
                            ? secondItem.ItemNote
                            : $"{existingItem.ItemNote}; {secondItem.ItemNote}";
                        existingItem.UpdatedAt = DateTime.UtcNow;
                        repoOrderItem.Update(existingItem);
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
                var allFirstOrderItems = await repoOrderItem.Query()
                    .Include(oi => oi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                    .Where(oi => oi.OrderId == firstOrder.OrderId)
                    .ToListAsync(cancellationToken);

                // Assuming GetTotalPrice() calculates the total price of the item including options
                firstOrder.TotalAmount = allFirstOrderItems.Sum(oi => oi.GetTotalPrice());
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

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                _logger.LogInformation(
                        "Successfully merged Order {SecondOrderCode} into {FirstOrderCode}. New TotalAmount: {TotalAmount}",
                        secondOrder.OrderCode,
                        firstOrder.OrderCode,
                        firstOrder.TotalAmount
                    );
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

            var response = _mapper.Map<MergeOrderResponse>(firstOrder);
            return Result<MergeOrderResponse>.Success(response);
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

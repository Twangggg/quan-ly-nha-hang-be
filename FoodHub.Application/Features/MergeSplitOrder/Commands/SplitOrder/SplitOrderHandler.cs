using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
        private readonly ISignalRService _signalRService;
        private readonly ILogger<SplitOrderHandler> _logger;
        private readonly IMapper _mapper;

        public SplitOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            IMapper mapper,
            ILogger<SplitOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<SplitOrderResponse>> Handle(
            SplitOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning(
                    "Unauthorized split attempt for Order {SourceOrderId}",
                    request.SourceOrderId
                );
                return Result<SplitOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                 "Starting split operation: SourceOrder={SourceOrderId}, DestinationOrder={DestinationOrderId}, DestinationTable={DestinationTableId}, DestinationReservation={DestinationReservationId}, ItemCount={ItemCount}, User={UserId}",
                 request.SourceOrderId,
                 request.DestinationOrderId,
                 request.DestinationTableId,
                 request.DestinationReservationId,
                 request.ItemsToSplit.Count,
                 auditorId
             );

            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var orderRepository = _unitOfWork.Repository<Order>();
            var orderItemRepository = _unitOfWork.Repository<OrderItem>();
            var tableRepository = _unitOfWork.Repository<Table>();
            var auditLogRepository = _unitOfWork.Repository<OrderAuditLog>();

            var sourceOrder = await orderRepository
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.SourceOrderId, cancellationToken);

            if (sourceOrder is null)
            {
                _logger.LogWarning(
                    "Source order {OrderId} was not found for split.",
                    request.SourceOrderId
                );
                return Result<SplitOrderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Order.NotFound, request.SourceOrderId)
                );
            }

            if (sourceOrder.OrderType != OrderType.DineIn || !sourceOrder.IsActive())
            {
                _logger.LogWarning(
                    "Split rejected because source order {OrderId} is not an active dine-in order. Status={Status}",
                    sourceOrder.OrderId,
                    sourceOrder.Status
                );
                return Result<SplitOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );
            }

            foreach (var itemToSplit in request.ItemsToSplit)
            {
                var orderItem = sourceOrder.OrderItems.FirstOrDefault(oi =>
                    oi.OrderItemId == itemToSplit.OrderItemId
                );

                if (orderItem is null)
                {
                    _logger.LogWarning(
                        "Split rejected because OrderItem {OrderItemId} does not belong to Order {OrderId}",
                        itemToSplit.OrderItemId,
                        sourceOrder.OrderId
                    );
                    return Result<SplitOrderResponse>.NotFound(
                        _messageService.GetMessage(
                            MessageKeys.OrderItem.NotFound,
                            itemToSplit.OrderItemId
                        )
                    );
                }

                if (!orderItem.CanBeMoved())
                {
                    _logger.LogWarning(
                        "Split rejected because OrderItem {OrderItemId} is not movable. Status={Status}",
                        orderItem.OrderItemId,
                        orderItem.Status
                    );
                    return Result<SplitOrderResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                        ResultErrorType.BadRequest
                    );
                }

                if (itemToSplit.QuantityToSplit > orderItem.Quantity)
                {
                    _logger.LogWarning(
                        "Split rejected because requested quantity {Quantity} exceeds current quantity {CurrentQuantity} for OrderItem {OrderItemId}",
                        itemToSplit.QuantityToSplit,
                        orderItem.Quantity,
                        orderItem.OrderItemId
                    );
                    return Result<SplitOrderResponse>.Failure(
                        _messageService.GetMessage(
                            MessageKeys.OrderItem.InvalidQuantity,
                            itemToSplit.OrderItemId
                        ),
                        ResultErrorType.BadRequest
                    );
                }
            }

            var now = DateTime.UtcNow;
            var createdNewOrder = false;
            Order? destinationOrder = null;
            Table? destinationTable = null;

            if (request.DestinationOrderId.HasValue)
            {
                if (request.DestinationOrderId.Value == sourceOrder.OrderId)
                {
                    _logger.LogWarning(
                        "Split rejected because destination order is the same as source order. OrderId={OrderId}",
                        sourceOrder.OrderId
                    );
                    return Result<SplitOrderResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                        ResultErrorType.BadRequest
                    );
                }

                destinationOrder = await orderRepository
                    .Query()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.OptionGroups)
                            .ThenInclude(og => og.OptionValues)
                    .FirstOrDefaultAsync(
                        o => o.OrderId == request.DestinationOrderId.Value,
                        cancellationToken
                    );

                if (destinationOrder is null)
                {
                    _logger.LogWarning(
                        "Destination order {OrderId} was not found for split.",
                        request.DestinationOrderId.Value
                    );
                    return Result<SplitOrderResponse>.NotFound(
                        _messageService.GetMessage(
                            MessageKeys.Order.NotFound,
                            request.DestinationOrderId.Value
                        )
                    );
                }

                if (destinationOrder.OrderType != OrderType.DineIn || !destinationOrder.IsActive())
                {
                    _logger.LogWarning(
                        "Split rejected because destination order {OrderId} is not an active dine-in order. Status={Status}",
                        destinationOrder.OrderId,
                        destinationOrder.Status
                    );
                    return Result<SplitOrderResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                        ResultErrorType.BadRequest
                    );
                }

                if (destinationOrder.TableId.HasValue)
                {
                    destinationTable = await tableRepository
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(
                            t => t.TableId == destinationOrder.TableId.Value,
                            cancellationToken
                        );
                }
            }
            else
            {
                if (!request.DestinationTableId.HasValue)
                {
                    _logger.LogWarning(
                        "Split rejected because no destination order or table was supplied for Order {OrderId}",
                        sourceOrder.OrderId
                    );
                    return Result<SplitOrderResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                        ResultErrorType.BadRequest
                    );
                }

                destinationTable = await tableRepository
                    .Query()
                    .Include(t => t.Orders)
                    .FirstOrDefaultAsync(
                        t => t.TableId == request.DestinationTableId.Value,
                        cancellationToken
                    );

                if (destinationTable is null)
                {
                    _logger.LogWarning(
                        "Destination table {TableId} was not found for split.",
                        request.DestinationTableId.Value
                    );
                    return Result<SplitOrderResponse>.NotFound(
                        _messageService.GetMessage(
                            MessageKeys.Table.NotFound,
                            request.DestinationTableId.Value
                        )
                    );
                }

                destinationOrder = await orderRepository
                    .Query()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.OptionGroups)
                            .ThenInclude(og => og.OptionValues)
                    .FirstOrDefaultAsync(
                        o =>
                            o.TableId == destinationTable.TableId
                            && o.Status == OrderStatus.Serving
                            && o.OrderId != sourceOrder.OrderId,
                        cancellationToken
                    );

                if (destinationOrder is null)
                {
                    if (destinationTable.Status != TableStatus.Available)
                    {
                        _logger.LogWarning(
                            "Split rejected because destination table {TableId} is not available. Status={Status}",
                            destinationTable.TableId,
                            destinationTable.Status
                        );
                        return Result<SplitOrderResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Table.NotAvailable),
                            ResultErrorType.BadRequest
                        );
                    }

                    createdNewOrder = true;
                    destinationOrder = FoodHub.Domain.Entities.Order.CreateSplitOrder(
                        await GenerateOrderCodeAsync(cancellationToken),
                        sourceOrder,
                        destinationTable.TableId,
                        request.DestinationReservationId,
                        now,
                        auditorId
                    );

                    await orderRepository.AddAsync(destinationOrder);

                    // Nếu tách sang bàn mới và có ReservationId mới, cần đảm bảo Reservation đó cũng trỏ về bàn này
                    if (
                        request.DestinationReservationId.HasValue
                        && request.DestinationReservationId != sourceOrder.ReservationId
                    )
                    {
                        var destReservation = await reservationRepository.GetByIdAsync(
                            request.DestinationReservationId.Value
                        );
                        if (destReservation != null)
                        {
                            destReservation.TableId = destinationTable.TableId;
                            destReservation.Status = ReservationStatus.CheckIn;
                            destReservation.UpdatedAt = now;
                            destReservation.UpdatedBy = auditorId;
                            reservationRepository.Update(destReservation);
                        }
                    }
                }
            }

            if (destinationOrder is null)
            {
                _logger.LogWarning(
                    "Split rejected because destination order resolution returned null for source order {OrderId}",
                    sourceOrder.OrderId
                );
                return Result<SplitOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var splitResult = sourceOrder.SplitItemsTo(
                    destinationOrder,
                    request
                        .ItemsToSplit.Select(item => new OrderItemSplitRequest(
                            item.OrderItemId,
                            item.QuantityToSplit
                        ))
                        .ToList(),
                    now,
                    auditorId
                );

                if (!splitResult.IsSuccess || splitResult.Value is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogWarning(
                        "Domain split rejected for source order {SourceOrderId}. Error={Error}",
                        sourceOrder.OrderId,
                        splitResult.ErrorCode
                    );
                    return Result<SplitOrderResponse>.Failure(
                        _messageService.GetMessage(
                            splitResult.ErrorCode ?? MessageKeys.Order.InvalidActionWithStatus
                        ),
                        ResultErrorType.BadRequest
                    );
                }

                foreach (var deletedItem in splitResult.Value.DeletedSourceItems)
                {
                    orderItemRepository.Delete(deletedItem);
                }

                orderRepository.Update(sourceOrder);
                if (!createdNewOrder)
                {
                    orderRepository.Update(destinationOrder);
                }

                if (destinationTable == null && destinationOrder.TableId.HasValue)
                {
                    destinationTable = await tableRepository
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(
                            t => t.TableId == destinationOrder.TableId.Value,
                            cancellationToken
                        );
                }

                if (
                    destinationTable != null
                    && (createdNewOrder || destinationTable.Status != TableStatus.Occupied)
                )
                {
                    destinationTable.MarkAsOccupied(auditorId, now);
                    tableRepository.Update(destinationTable);
                }

                if (sourceOrder.TableId.HasValue && sourceOrder.TableId != destinationOrder.TableId)
                {
                    var sourceTable = await tableRepository
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(
                            t => t.TableId == sourceOrder.TableId.Value,
                            cancellationToken
                        );

                    if (sourceTable != null && sourceTable.SetAvailable())
                    {
                        sourceTable.UpdatedAt = now;
                        sourceTable.UpdatedBy = auditorId;
                        tableRepository.Update(sourceTable);
                    }
                }

                await auditLogRepository.AddAsync(
                    new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = sourceOrder.OrderId,
                        EmployeeId = auditorId,
                        Action = AuditLogActions.SplitOrder,
                        CreatedAt = now,
                        NewValue =
                            $"{{\"destinationOrderId\":\"{destinationOrder.OrderId}\",\"createdNewOrder\":{createdNewOrder.ToString().ToLowerInvariant()}}}",
                    }
                );

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                if (destinationTable != null)
                {
                    await _signalRService.NotifyTableStatusChangedAsync(destinationTable.TableId, destinationTable.Status.ToString());
                }

                if (sourceOrder.TableId.HasValue && sourceOrder.TableId != destinationOrder.TableId)
                {
                    // For the source table, we don't have the sourceTable object here unless it was loaded in line 398.
                    // But we can just query its current status or pass "Available" if we know it changed.
                    // Instead of full query again, we can just trigger a refresh so the FE pulls exactly what's in DB,
                    // or we check if we actually altered it above. Since we did above, let's just trigger a re-check or assuming we have it.
                    // A safer bet is just querying its status quickly.
                    var sTableStatus = await tableRepository.Query()
                        .Where(t => t.TableId == sourceOrder.TableId.Value)
                        .Select(t => t.Status.ToString())
                        .FirstOrDefaultAsync(cancellationToken);
                    if (sTableStatus != null)
                    {
                        await _signalRService.NotifyTableStatusChangedAsync(sourceOrder.TableId.Value, sTableStatus);
                    }
                }

                _logger.LogInformation(
                    "Successfully split items from Order {SourceOrderCode} to Order {DestinationOrderCode}. SourceAmount={SourceAmount}, DestinationAmount={DestinationAmount}",
                    sourceOrder.OrderCode,
                    destinationOrder.OrderCode,
                    sourceOrder.TotalAmount,
                    destinationOrder.TotalAmount
                );

                return Result<SplitOrderResponse>.Success(
                    new SplitOrderResponse
                    {
                        SourceOrderId = sourceOrder.OrderId,
                        SourceOrderCode = sourceOrder.OrderCode,
                        SourceOrderTotalAmount = sourceOrder.TotalAmount,
                        SourceOrderItems = _mapper.Map<List<SplitOrderItemDto>>(
                            sourceOrder.OrderItems.ToList()
                        ),
                        DestinationOrderId = destinationOrder.OrderId,
                        DestinationOrderCode = destinationOrder.OrderCode,
                        DestinationOrderTotalAmount = destinationOrder.TotalAmount,
                        DestinationOrderItems = _mapper.Map<List<SplitOrderItemDto>>(
                            destinationOrder.OrderItems.ToList()
                        ),
                        DestinationTableId = destinationOrder.TableId,
                        CreatedNewOrder = createdNewOrder,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Failed to split Order {SourceOrderId}",
                    request.SourceOrderId
                );
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

            var sequenceNumber = 1;
            if (lastOrder != null)
            {
                var parts = lastOrder.OrderCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out var lastSequence))
                {
                    sequenceNumber = lastSequence + 1;
                }
            }

            return $"{prefix}{sequenceNumber:D4}";
        }
    }
}

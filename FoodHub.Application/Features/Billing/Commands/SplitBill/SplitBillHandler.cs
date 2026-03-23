using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Commands.SplitBill
{
    public class SplitBillHandler : IRequestHandler<SplitBillCommand, Result<SplitBillResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<SplitBillHandler> _logger;
        private readonly IMapper _mapper;

        public SplitBillHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<SplitBillHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<SplitBillResponse>> Handle(
            SplitBillCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<SplitBillResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var orderRepo = _unitOfWork.Repository<Order>();
            var orderItemRepo = _unitOfWork.Repository<OrderItem>();
            var auditRepo = _unitOfWork.Repository<OrderAuditLog>();

            var sourceOrder = await orderRepo
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (sourceOrder is null)
                return Result<SplitBillResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Order.NotFound, request.OrderId)
                );

            if (sourceOrder.OrderType != OrderType.DineIn || !sourceOrder.IsActive())
                return Result<SplitBillResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );

            foreach (var itemToSplit in request.ItemsToSplit)
            {
                var orderItem = sourceOrder.OrderItems.FirstOrDefault(x =>
                    x.OrderItemId == itemToSplit.OrderItemId
                );
                if (orderItem is null)
                    return Result<SplitBillResponse>.NotFound(
                        _messageService.GetMessage(
                            MessageKeys.OrderItem.NotFound,
                            itemToSplit.OrderItemId
                        )
                    );
                if (!orderItem.CanBeMoved())
                    return Result<SplitBillResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                        ResultErrorType.BadRequest
                    );
                if (
                    itemToSplit.QuantityToSplit <= 0
                    || itemToSplit.QuantityToSplit > orderItem.Quantity
                )
                    return Result<SplitBillResponse>.Failure(
                        _messageService.GetMessage(
                            MessageKeys.OrderItem.InvalidQuantity,
                            itemToSplit.OrderItemId
                        ),
                        ResultErrorType.BadRequest
                    );
            }

            var now = DateTime.UtcNow;
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var destinationOrder = new Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderCode = await GenerateOrderCodeAsync(cancellationToken),
                    OrderType = sourceOrder.OrderType,
                    Status = OrderStatus.Serving,
                    TableId = sourceOrder.TableId,
                    Note = $"Split bill from Order {sourceOrder.OrderCode}",
                    TotalAmount = 0,
                    IsPriority = sourceOrder.IsPriority,
                    CreatedAt = now,
                    CreatedBy = auditorId,
                };

                await orderRepo.AddAsync(destinationOrder);

                var splitResult = sourceOrder.SplitItemsTo(
                    destinationOrder,
                    request
                        .ItemsToSplit.Select(x => new OrderItemSplitRequest(
                            x.OrderItemId,
                            x.QuantityToSplit
                        ))
                        .ToList(),
                    now,
                    auditorId
                );

                if (!splitResult.IsSuccess || splitResult.Value is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<SplitBillResponse>.Failure(
                        _messageService.GetMessage(
                            splitResult.ErrorCode ?? MessageKeys.Billing.SplitBillFailed
                        ),
                        ResultErrorType.BadRequest
                    );
                }

                foreach (var deletedItem in splitResult.Value.DeletedSourceItems)
                    orderItemRepo.Delete(deletedItem);

                orderRepo.Update(sourceOrder);

                await auditRepo.AddAsync(
                    new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = sourceOrder.OrderId,
                        EmployeeId = auditorId,
                        Action = OrderAuditActions.SplitBill,
                        CreatedAt = now,
                        NewValue =
                            $"{{\"destinationOrderId\":\"{destinationOrder.OrderId}\",\"splitBill\":true}}",
                    }
                );

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<SplitBillResponse>.Success(
                    new SplitBillResponse
                    {
                        SourceOrderId = sourceOrder.OrderId,
                        SourceOrderCode = sourceOrder.OrderCode,
                        SourceOrderTotalAmount = sourceOrder.TotalAmount,
                        SourceOrderItems = _mapper.Map<List<SplitBillItemDto>>(
                            sourceOrder.OrderItems.ToList()
                        ),
                        DestinationOrderId = destinationOrder.OrderId,
                        DestinationOrderCode = destinationOrder.OrderCode,
                        DestinationOrderTotalAmount = destinationOrder.TotalAmount,
                        DestinationOrderItems = _mapper.Map<List<SplitBillItemDto>>(
                            destinationOrder.OrderItems.ToList()
                        ),
                        DestinationTableId = destinationOrder.TableId,
                    }
                );
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"ORD-{datePrefix}-";
            var latest = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .Select(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);

            var nextNumber = 1;
            if (!string.IsNullOrWhiteSpace(latest))
            {
                var suffix = latest.Substring(prefix.Length);
                if (int.TryParse(suffix, out var current))
                    nextNumber = current + 1;
            }
            return $"{prefix}{nextNumber:D4}";
        }
    }
}

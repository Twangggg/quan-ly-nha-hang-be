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

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    public class MergeOrderHandler : IRequestHandler<MergeOrderCommand, Result<MergeOrderResponse>>
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
            ILogger<MergeOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<MergeOrderResponse>> Handle(
            MergeOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning(
                    "Unauthorized merge attempt for orders {FirstOrderId} and {SecondOrderId}",
                    request.FirstOrder,
                    request.SecondOrder
                );
                return Result<MergeOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            if (request.FirstOrder == request.SecondOrder)
            {
                _logger.LogWarning(
                    "Merge rejected because source and destination order are the same. OrderId={OrderId}",
                    request.FirstOrder
                );
                return Result<MergeOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                    ResultErrorType.BadRequest
                );
            }

            _logger.LogInformation(
                "Starting merge operation: FirstOrder={FirstOrderId}, SecondOrder={SecondOrderId}, User={UserId}",
                request.FirstOrder,
                request.SecondOrder,
                auditorId
            );

            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var orderRepository = _unitOfWork.Repository<Order>();
            var orderItemRepository = _unitOfWork.Repository<OrderItem>();
            var tableRepository = _unitOfWork.Repository<Table>();
            var auditLogRepository = _unitOfWork.Repository<OrderAuditLog>();

            var firstOrder = await orderRepository
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.FirstOrder, cancellationToken);

            if (firstOrder is null)
            {
                _logger.LogWarning(
                    "Primary order {OrderId} was not found for merge.",
                    request.FirstOrder
                );
                return Result<MergeOrderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Order.NotFound, request.FirstOrder)
                );
            }

            var secondOrder = await orderRepository
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.SecondOrder, cancellationToken);

            if (secondOrder is null)
            {
                _logger.LogWarning(
                    "Secondary order {OrderId} was not found for merge.",
                    request.SecondOrder
                );
                return Result<MergeOrderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Order.NotFound, request.SecondOrder)
                );
            }

            if (
                firstOrder.OrderType != OrderType.DineIn
                || secondOrder.OrderType != OrderType.DineIn
                || !firstOrder.IsActive()
                || !secondOrder.IsActive()
            )
            {
                _logger.LogWarning(
                    "Merge rejected because orders are not active dine-in orders. FirstStatus={FirstStatus}, SecondStatus={SecondStatus}",
                    firstOrder.Status,
                    secondOrder.Status
                );
                return Result<MergeOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;
                var mergeResult = firstOrder.MergeFrom(secondOrder, now, auditorId);

                if (!mergeResult.IsSuccess || mergeResult.Value is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogWarning(
                        "Domain merge rejected for orders {FirstOrderId} and {SecondOrderId}. Error={Error}",
                        request.FirstOrder,
                        request.SecondOrder,
                        mergeResult.ErrorCode
                    );
                    return Result<MergeOrderResponse>.Failure(
                        _messageService.GetMessage(
                            mergeResult.ErrorCode ?? MessageKeys.Order.InvalidActionWithStatus
                        ),
                        ResultErrorType.BadRequest
                    );
                }

                foreach (var deletedItem in mergeResult.Value.DeletedSourceItems)
                {
                    orderItemRepository.Delete(deletedItem);
                }

                orderRepository.Update(firstOrder);
                orderRepository.Update(secondOrder);

                if (secondOrder.TableId.HasValue && secondOrder.TableId != firstOrder.TableId)
                {
                    var secondTable = await tableRepository
                        .Query()
                        .Include(t => t.Orders)
                        .FirstOrDefaultAsync(
                            t => t.TableId == secondOrder.TableId.Value,
                            cancellationToken
                        );

                    // Ghi chú: Chúng ta KHÔNG giải phóng bàn hoặc reservation của đơn bị gộp (secondOrder) 
                    // vì theo ý kiến người dùng, khách vẫn có thể ngồi tại bàn đó cho đến khi thanh toán xong toàn bộ.
                    // Table của secondOrder sẽ ở trạng thái Occupied nhưng không còn Order gắn trực tiếp (vì đã gộp vào firstOrder).
                }

                await auditLogRepository.AddAsync(
                    new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = firstOrder.OrderId,
                        EmployeeId = auditorId,
                        Action = AuditLogActions.MergeOrder,
                        CreatedAt = now,
                        NewValue =
                            $"{{\"mergedFromOrderId\":\"{secondOrder.OrderId}\",\"mergedOrderCode\":\"{firstOrder.OrderCode}\"}}",
                    }
                );

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully merged Order {SecondOrderCode} into {FirstOrderCode}. New TotalAmount={TotalAmount}",
                    secondOrder.OrderCode,
                    firstOrder.OrderCode,
                    firstOrder.TotalAmount
                );

                return Result<MergeOrderResponse>.Success(
                    new MergeOrderResponse
                    {
                        MergedOrderId = firstOrder.OrderId,
                        MergedOrderCode = firstOrder.OrderCode,
                        MergedOrderTotalAmount = firstOrder.TotalAmount,
                        Items = _mapper.Map<List<OrderItemDto>>(firstOrder.OrderItems.ToList()),
                    }
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
        }
    }
}

using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    public class ChangeOrderTableHandler
        : IRequestHandler<ChangeOrderTableCommand, Result<ChangeOrderTableResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<ChangeOrderTableHandler> _logger;

        public ChangeOrderTableHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ISignalRService signalRService,
            ILogger<ChangeOrderTableHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _signalRService = signalRService;
            _logger = logger;
        }

        public async Task<Result<ChangeOrderTableResponse>> Handle(
            ChangeOrderTableCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var orderRepository = _unitOfWork.Repository<Order>();
            var tableRepository = _unitOfWork.Repository<Table>();
            var auditRepository = _unitOfWork.Repository<OrderAuditLog>();

            var order = await orderRepository
                .Query()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (order.OrderType != OrderType.DineIn || !order.IsActive() || !order.TableId.HasValue)
            {
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );
            }

            if (order.TableId == request.TableId)
            {
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Table.SameAsCurrentTable),
                    ResultErrorType.BadRequest
                );
            }

            var tables = await tableRepository
                .Query()
                .Include(t => t.Orders)
                .Where(t => t.TableId == order.TableId || t.TableId == request.TableId)
                .ToListAsync(cancellationToken);

            var currentTable = tables.FirstOrDefault(t => t.TableId == order.TableId);
            var newTable = tables.FirstOrDefault(t => t.TableId == request.TableId);

            if (currentTable == null || newTable == null)
            {
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Table.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (newTable.Status != TableStatus.Available)
            {
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Table.NotAvailable),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            var committed = false;

            try
            {
                var now = DateTime.UtcNow;

                currentTable.DetachOrder(order.OrderId, auditorId, now);
                order.ChangeTable(request.TableId, now, auditorId);
                newTable.AttachOrder(order, auditorId, now);
                currentTable.ReleaseIfNoActiveOrders(auditorId, now);

                orderRepository.Update(order);
                tableRepository.Update(currentTable);
                tableRepository.Update(newTable);

                await auditRepository.AddAsync(
                    new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        EmployeeId = auditorId.Value,
                        Action = AuditLogActions.ChangeOrderTable,
                        OldValue = System.Text.Json.JsonSerializer.Serialize(
                            new { tableId = currentTable.TableId }
                        ),
                        NewValue = System.Text.Json.JsonSerializer.Serialize(
                            new { tableId = newTable.TableId }
                        ),
                        CreatedAt = now,
                    }
                );

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                committed = true;

                try
                {
                    await _cacheService.RemoveByPatternAsync(
                        CacheKey.TableList + "*",
                        cancellationToken
                    );
                    await _cacheService.RemoveByPatternAsync(
                        string.Format(CacheKey.TableListByArea, "*"),
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Order table change committed but table cache invalidation failed for OrderId {OrderId}",
                        request.OrderId
                    );
                }

                await _signalRService.NotifyTableStatusChangedAsync(
                    currentTable.TableId,
                    currentTable.Status.ToString()
                );
                await _signalRService.NotifyTableStatusChangedAsync(
                    newTable.TableId,
                    newTable.Status.ToString()
                );

                var response = new ChangeOrderTableResponse
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    OldTableId = currentTable.TableId,
                    OldTableName = currentTable.TableNumber.ToString(),
                    NewTableId = newTable.TableId,
                    NewTableName = newTable.TableNumber.ToString(),
                };

                return Result<ChangeOrderTableResponse>.Success(response);
            }
            finally
            {
                if (!committed)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
            }
        }
    }
}

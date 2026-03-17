using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces;
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
        private readonly ILogger<ChangeOrderTableHandler> _logger;

        public ChangeOrderTableHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<ChangeOrderTableHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
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

            try
            {
                var now = DateTime.UtcNow;

                // Remove order from current table and attach to new table snapshot
                if (currentTable.Orders != null)
                {
                    var toRemove = currentTable.Orders.FirstOrDefault(o =>
                        o.OrderId == order.OrderId
                    );
                    if (toRemove != null)
                    {
                        currentTable.Orders.Remove(toRemove);
                    }
                }

                order.ChangeTable(request.TableId, now, auditorId);

                if (
                    newTable.Orders != null
                    && !newTable.Orders.Any(o => o.OrderId == order.OrderId)
                )
                {
                    newTable.Orders.Add(order);
                }

                newTable.MarkAsOccupied(auditorId, now);

                if (currentTable.SetAvailable())
                {
                    currentTable.UpdatedAt = now;
                    currentTable.UpdatedBy = auditorId;
                }

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
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Failed to change order table for OrderId {OrderId}",
                    request.OrderId
                );
                throw;
            }
        }
    }
}

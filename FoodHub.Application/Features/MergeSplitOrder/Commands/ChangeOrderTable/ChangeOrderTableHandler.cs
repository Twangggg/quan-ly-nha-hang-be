using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
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
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning(
                    "Unauthorized change-table attempt for Order {OrderId}",
                    request.OrderId
                );
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                "Starting change table operation: Order={OrderId}, NewTable={TableId}, User={UserId}",
                request.OrderId,
                request.TableId,
                auditorId
            );

            var orderRepository = _unitOfWork.Repository<Order>();
            var tableRepository = _unitOfWork.Repository<Table>();
            var auditLogRepository = _unitOfWork.Repository<OrderAuditLog>();

            var currentOrder = await orderRepository
                .Query()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (currentOrder is null)
            {
                _logger.LogWarning(
                    "Change table rejected because Order {OrderId} was not found.",
                    request.OrderId
                );
                return Result<ChangeOrderTableResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Order.NotFound)
                );
            }

            if (currentOrder.OrderType != OrderType.DineIn || !currentOrder.IsActive())
            {
                _logger.LogWarning(
                    "Change table rejected because Order {OrderId} is not an active dine-in order. Status={Status}",
                    currentOrder.OrderId,
                    currentOrder.Status
                );
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus),
                    ResultErrorType.BadRequest
                );
            }

            if (!currentOrder.TableId.HasValue)
            {
                _logger.LogWarning(
                    "Change table rejected because Order {OrderId} has no current table.",
                    currentOrder.OrderId
                );
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidAction),
                    ResultErrorType.BadRequest
                );
            }

            var oldTable = await tableRepository
                .Query()
                .Include(t => t.Orders)
                .FirstOrDefaultAsync(t => t.TableId == currentOrder.TableId.Value, cancellationToken);

            var newTable = await tableRepository
                .Query()
                .Include(t => t.Orders)
                .FirstOrDefaultAsync(t => t.TableId == request.TableId, cancellationToken);

            if (newTable is null || oldTable is null)
            {
                _logger.LogWarning(
                    "Change table rejected because source or destination table was not found. OldTableId={OldTableId}, NewTableId={NewTableId}",
                    currentOrder.TableId,
                    request.TableId
                );
                return Result<ChangeOrderTableResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Table.NotFound)
                );
            }

            if (oldTable.TableId == newTable.TableId)
            {
                _logger.LogWarning(
                    "Change table rejected because Order {OrderId} already belongs to Table {TableId}",
                    currentOrder.OrderId,
                    newTable.TableId
                );
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Table.SameAsCurrentTable),
                    ResultErrorType.BadRequest
                );
            }

            if (newTable.Status != TableStatus.Available)
            {
                _logger.LogWarning(
                    "Change table rejected because destination Table {TableId} is not available. Status={Status}",
                    newTable.TableId,
                    newTable.Status
                );
                return Result<ChangeOrderTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Table.NotAvailable),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                currentOrder.ChangeTable(newTable.TableId, now, auditorId);
                orderRepository.Update(currentOrder);
                oldTable.Orders.Remove(currentOrder);

                if (oldTable.SetAvailable())
                {
                    oldTable.UpdatedAt = now;
                    oldTable.UpdatedBy = auditorId;
                    tableRepository.Update(oldTable);
                }

                newTable.MarkAsOccupied(auditorId, now);
                tableRepository.Update(newTable);

                await auditLogRepository.AddAsync(
                    new OrderAuditLog
                    {
                        LogId = Guid.NewGuid(),
                        OrderId = currentOrder.OrderId,
                        EmployeeId = auditorId,
                        Action = AuditLogActions.ChangeOrderTable,
                        CreatedAt = now,
                        NewValue =
                            $"{{\"oldTableId\":\"{oldTable.TableId}\",\"newTableId\":\"{newTable.TableId}\"}}",
                    }
                );

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully changed Order {OrderCode} from Table {OldTable} to Table {NewTable}",
                    currentOrder.OrderCode,
                    oldTable.GetTableName(),
                    newTable.GetTableName()
                );

                return Result<ChangeOrderTableResponse>.Success(
                    new ChangeOrderTableResponse
                    {
                        OrderId = currentOrder.OrderId,
                        OrderCode = currentOrder.OrderCode,
                        OldTableId = oldTable.TableId,
                        OldTableName = oldTable.GetTableName(),
                        NewTableId = newTable.TableId,
                        NewTableName = newTable.GetTableName(),
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Failed to change table for Order {OrderId} to Table {TableId}",
                    request.OrderId,
                    request.TableId
                );
                throw;
            }
        }
    }
}

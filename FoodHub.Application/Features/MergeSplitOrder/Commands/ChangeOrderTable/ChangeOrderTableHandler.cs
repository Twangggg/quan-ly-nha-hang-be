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
    public class ChangeOrderTableHandler : IRequestHandler<ChangeOrderTableCommand, Result<ChangeOrderTableResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<ChangeOrderTableHandler> _logger;

        public ChangeOrderTableHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ILogger<ChangeOrderTableHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<ChangeOrderTableResponse>> Handle(ChangeOrderTableCommand request, CancellationToken cancellationToken)
        {
            // Initialize repositories
            var repoOrder = _unitOfWork.Repository<Order>();
            var repoTable = _unitOfWork.Repository<Table>();

            // Attempt to parse user ID for auditing
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            _logger.LogInformation(
                "Starting change table operation: Order={OrderId}, NewTable={TableId}, User={UserId}",
                request.OrderId,
                request.TableId,
                auditorId
            );

            var currentOrder = await repoOrder.Query()
                .Include(oo => oo.Table)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
            if (currentOrder is null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound);
                return Result<ChangeOrderTableResponse>.NotFound(errorMessage);
            }
            if (currentOrder.Status != OrderStatus.Serving)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.InvalidStatus, new { Status = currentOrder.Status });
                return Result<ChangeOrderTableResponse>.Failure(errorMessage, ResultErrorType.BadRequest);
            }

            var newTable = await repoTable.Query()
                .FirstOrDefaultAsync(t => t.TableId == request.TableId, cancellationToken);
            var oldTable = currentOrder.Table;
            if (newTable is null || oldTable is null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Table.NotFound);
                return Result<ChangeOrderTableResponse>.NotFound(errorMessage);
            }
            if (oldTable.TableId == newTable.TableId)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Table.SameAsCurrentTable);
                return Result<ChangeOrderTableResponse>.Failure(errorMessage, ResultErrorType.BadRequest);
            }
            if (newTable.Status != TableStatus.Available)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.TableAlreadyOccupied);
                return Result<ChangeOrderTableResponse>.Failure(errorMessage, ResultErrorType.BadRequest);
            }

            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Update order's table
                currentOrder.TableId = newTable.TableId;
                currentOrder.UpdatedAt = DateTime.UtcNow;
                currentOrder.UpdatedBy = auditorId;
                repoOrder.Update(currentOrder);

                // Update old table status to Available
                if (oldTable.SetAvailable())
                {
                    oldTable.UpdatedAt = DateTime.UtcNow;
                    oldTable.UpdatedBy = auditorId;
                    repoTable.Update(oldTable);
                }

                // Update new table status to Occupied
                newTable.Status = TableStatus.Occupied;
                newTable.UpdatedAt = DateTime.UtcNow;
                newTable.UpdatedBy = auditorId;
                repoTable.Update(newTable);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully changed Order {OrderCode} from Table {OldTable} to Table {NewTable}",
                    currentOrder.OrderCode,
                    oldTable.GetTableName(),
                    newTable.GetTableName()
                );

                var response = new ChangeOrderTableResponse
                {
                    OrderId = currentOrder.OrderId,
                    OrderCode = currentOrder.OrderCode,
                    OldTableId = oldTable.TableId,
                    OldTableName = oldTable.GetTableName(),
                    NewTableId = newTable.TableId,
                    NewTableName = newTable.GetTableName()
                };

                return Result<ChangeOrderTableResponse>.Success(response);
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

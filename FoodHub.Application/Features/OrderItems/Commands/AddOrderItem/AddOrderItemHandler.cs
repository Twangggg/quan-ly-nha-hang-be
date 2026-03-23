using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Application.Features.Options.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemHandler : IRequestHandler<AddOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;

        public AddOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
        }

        public async Task<Result<Guid>> Handle(
            AddOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUserService.GetUserIdAsGuid();
            if (userId == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Domain.Entities.Order>()
                .Query()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (!order.IsActive())
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus)
                );
            }

            var menuItem = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .Include(m => m.MenuItemOptionGroups)
                    .ThenInclude(miog => miog.OptionGroup)
                        .ThenInclude(og => og.OptionItems)
                .FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.NotFound)
                );
            }

            if (menuItem.IsOutOfStock)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.OutOfStock)
                );
            }

            // Prepare Domain-ready options
            var selectionValidation = OptionSelectionValidation.ValidateForMenuItem(
                menuItem,
                request.SelectedOptions
                    ?.Select(
                        x =>
                            new RequestedOptionSelection(
                                x.OptionGroupId,
                                x.SelectedValues
                                    .Select(v => new RequestedOptionValue(v.OptionItemId, v.Quantity, v.Note))
                                    .ToList()
                            )
                    )
                    .ToList(),
                _messageService
            );
            if (!selectionValidation.IsSuccess)
            {
                return Result<Guid>.Failure(
                    selectionValidation.Error!,
                    selectionValidation.ErrorType
                );
            }

            var domainOptions = selectionValidation
                .Data!.Select(x => (x.Assignment, x.Group, x.Selections))
                .ToList();

            // Delegate logic to Domain Entity
            var result = order.AddOrUpdateItem(
                menuItem,
                request.Quantity,
                request.Note,
                domainOptions
            );

            if (!result.IsNew && request.Reason == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.ReasonRequired),
                    ResultErrorType.BadRequest
                );
            }

            // Audit & Save
            var auditLog = OrderAuditLog.CreateOrderItemAdded(
                order.OrderId,
                userId.Value,
                result.Item.OrderItemId,
                result.IsNew,
                request.Quantity,
                request.Reason
            );

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Notify KDS
            await _signalRService.NotifyOrderItemStatusChangedAsync(
                result.Item.OrderItemId,
                result.Item.Status,
                result.Item.StationSnapshot
            );

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

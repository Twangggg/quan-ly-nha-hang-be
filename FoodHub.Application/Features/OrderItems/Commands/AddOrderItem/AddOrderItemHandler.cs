using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
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
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn)
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

            if (order.Status != OrderStatus.Serving)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.InvalidAction)
                );
            }

            var menuItem = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .Include(m => m.OptionGroups)
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
            var domainOptions =
                new List<(
                    OptionGroup Group,
                    List<(OptionItem Item, int Quantity, string? Note)> Selections
                )>();
            if (request.SelectedOptions != null)
            {
                var optionGroupIds = request
                    .SelectedOptions.Select(og => og.OptionGroupId)
                    .ToList();
                var optionItemIds = request
                    .SelectedOptions.SelectMany(og => og.SelectedValues)
                    .Select(v => v.OptionItemId)
                    .ToList();

                var optionGroups = await _unitOfWork
                    .Repository<OptionGroup>()
                    .Query()
                    .Where(og => optionGroupIds.Contains(og.OptionGroupId))
                    .ToDictionaryAsync(og => og.OptionGroupId, cancellationToken);

                var optionItems = await _unitOfWork
                    .Repository<OptionItem>()
                    .Query()
                    .Where(oi => optionItemIds.Contains(oi.OptionItemId))
                    .ToDictionaryAsync(oi => oi.OptionItemId, cancellationToken);

                foreach (var optDto in request.SelectedOptions)
                {
                    if (optionGroups.TryGetValue(optDto.OptionGroupId, out var og))
                    {
                        var selections = optDto
                            .SelectedValues.Where(v => optionItems.ContainsKey(v.OptionItemId))
                            .Select(v => (optionItems[v.OptionItemId], v.Quantity, v.Note))
                            .ToList();

                        domainOptions.Add((og, selections));
                    }
                }
            }

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
            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = userId,
                Action = AuditLogActions.AddOrderItem,
                ChangeReason = request.Reason,
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Notify KDS
            _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                result.Item.OrderItemId,
                result.Item.Status,
                result.Item.StationSnapshot
            );

            return Result<Guid>.Success(order.OrderId);
        }
    }
}

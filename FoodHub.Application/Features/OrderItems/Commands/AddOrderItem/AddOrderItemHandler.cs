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
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces.Kds;
using AutoMapper;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemHandler : IRequestHandler<AddOrderItemCommand, Result<AddOrderItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly IKdsAutoPullService _kdsAutoPullService;
        private readonly IMapper _mapper;

        public AddOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            KdsPriorityCalculator priorityCalculator,
            IKdsSettingsProvider kdsSettingsProvider,
            IKdsAutoPullService kdsAutoPullService,
            IMapper mapper
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _priorityCalculator = priorityCalculator;
            _kdsSettingsProvider = kdsSettingsProvider;
            _kdsAutoPullService = kdsAutoPullService;
            _mapper = mapper;
        }

        public async Task<Result<AddOrderItemResponse>> Handle(
            AddOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUserService.GetUserIdAsGuid();
            if (userId == null)
            {
                return Result<AddOrderItemResponse>.Failure(
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
                .Include(x => x.Promotion)
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<AddOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (!order.IsActive())
            {
                return Result<AddOrderItemResponse>.Failure(
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
                return Result<AddOrderItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.NotFound)
                );
            }

            if (menuItem.IsOutOfStock)
            {
                return Result<AddOrderItemResponse>.Failure(
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
                return Result<AddOrderItemResponse>.Failure(
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

            // Auto-start cooking if capacity allows
            var availableSlots = await _kdsAutoPullService.GetAvailableSlotsAsync(new[] { result.Item.StationSnapshot }, cancellationToken);
            if (availableSlots.TryGetValue(result.Item.StationSnapshot, out int slots) && slots > 0)
            {
                result.Item.StartCooking();
            }

            if (!result.IsNew && request.Reason == null)
            {
                return Result<AddOrderItemResponse>.Failure(
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
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Notify KDS
            var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
            result.Item.Order = order; // Ensure Order navigation is set
            var response = KdsMappingHelper.MapToResponse(result.Item, _priorityCalculator, settings);
            await _signalRService.NotifyKdsItemUpdatedAsync(result.Item.StationSnapshot, response);

            await _signalRService.NotifyOrderItemStatusChangedAsync(
                result.Item.OrderItemId,
                result.Item.Status,
                result.Item.StationSnapshot
            );

            var apiResponse = _mapper.Map<AddOrderItemResponse>(order);
            apiResponse.NewOrderItemId = result.Item.OrderItemId;
            return Result<AddOrderItemResponse>.Success(apiResponse);
        }
    }
}

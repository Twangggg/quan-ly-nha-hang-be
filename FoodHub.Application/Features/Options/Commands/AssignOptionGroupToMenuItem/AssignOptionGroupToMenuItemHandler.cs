using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Commands.AssignOptionGroupToMenuItem
{
    public class AssignOptionGroupToMenuItemHandler
        : IRequestHandler<
            AssignOptionGroupToMenuItemCommand,
            Result<AssignOptionGroupToMenuItemResponse>
        >
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignOptionGroupToMenuItemHandler> _logger;
        private readonly IMessageService _messageService;

        public AssignOptionGroupToMenuItemHandler(
            IUnitOfWork unitOfWork,
            ILogger<AssignOptionGroupToMenuItemHandler> logger,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<AssignOptionGroupToMenuItemResponse>> Handle(
            AssignOptionGroupToMenuItemCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start assigning OptionGroupId={OptionGroupId} to MenuItemId={MenuItemId}",
                request.OptionGroupId,
                request.MenuItemId
            );
            var menuItemExists = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .AnyAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);
            if (!menuItemExists)
            {
                return Result<AssignOptionGroupToMenuItemResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.MenuItem.NotFound, request.MenuItemId)
                );
            }

            var optionGroup = await _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .FirstOrDefaultAsync(
                    x => x.OptionGroupId == request.OptionGroupId,
                    cancellationToken
                );
            if (optionGroup == null)
            {
                return Result<AssignOptionGroupToMenuItemResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.OptionGroup.NotFound, request.OptionGroupId)
                );
            }

            var existingAssignment = await _unitOfWork
                .Repository<MenuItemOptionGroup>()
                .Query()
                .FirstOrDefaultAsync(
                    x =>
                        x.MenuItemId == request.MenuItemId
                        && x.OptionGroupId == request.OptionGroupId,
                    cancellationToken
                );
            if (existingAssignment != null)
            {
                return Result<AssignOptionGroupToMenuItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.OptionGroup.AlreadyAssigned),
                    ResultErrorType.Conflict
                );
            }

            var assignment = MenuItemOptionGroup.Create(
                request.MenuItemId,
                request.OptionGroupId,
                optionGroup.OptionType,
                request.IsRequired,
                request.MinSelect,
                request.MaxSelect,
                request.SortOrder,
                request.IsVisible
            );

            await _unitOfWork.Repository<MenuItemOptionGroup>().AddAsync(assignment);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(
                "End assigning OptionGroupId={OptionGroupId} to MenuItemId={MenuItemId}",
                request.OptionGroupId,
                request.MenuItemId
            );

            return Result<AssignOptionGroupToMenuItemResponse>.Success(
                new AssignOptionGroupToMenuItemResponse
                {
                    MenuItemOptionGroupId = assignment.MenuItemOptionGroupId,
                    MenuItemId = assignment.MenuItemId,
                    OptionGroupId = assignment.OptionGroupId,
                    IsRequired = assignment.IsRequired,
                    MinSelect = assignment.MinSelect,
                    MaxSelect = assignment.MaxSelect,
                    SortOrder = assignment.SortOrder,
                    IsVisible = assignment.IsVisible,
                }
            );
        }
    }
}

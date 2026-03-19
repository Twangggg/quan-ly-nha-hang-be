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

namespace FoodHub.Application.Features.Options.Commands.UpdateMenuItemOptionGroup
{
    public class UpdateMenuItemOptionGroupHandler
        : IRequestHandler<
            UpdateMenuItemOptionGroupCommand,
            Result<UpdateMenuItemOptionGroupResponse>
        >
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateMenuItemOptionGroupHandler> _logger;
        private readonly IMessageService _messageService;

        public UpdateMenuItemOptionGroupHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateMenuItemOptionGroupHandler> logger,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<UpdateMenuItemOptionGroupResponse>> Handle(
            UpdateMenuItemOptionGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start updating MenuItemOptionGroup OptionGroupId={OptionGroupId} Config",
                request.MenuItemOptionGroupId
            );
            var assignment = await _unitOfWork
                .Repository<MenuItemOptionGroup>()
                .Query()
                .Include(x => x.OptionGroup)
                .FirstOrDefaultAsync(
                    x => x.MenuItemOptionGroupId == request.MenuItemOptionGroupId,
                    cancellationToken
                );
            if (assignment == null)
            {
                return Result<UpdateMenuItemOptionGroupResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.OptionGroup.NotFound, request.MenuItemOptionGroupId)
                );
            }

            assignment.UpdateConfiguration(
                assignment.OptionGroup.OptionType,
                request.IsRequired,
                request.MinSelect,
                request.MaxSelect,
                request.SortOrder,
                request.IsVisible
            );

            _unitOfWork.Repository<MenuItemOptionGroup>().Update(assignment);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(
                "End updating MenuItemOptionGroup OptionGroupId={OptionGroupId}",
                assignment.MenuItemOptionGroupId
            );

            return Result<UpdateMenuItemOptionGroupResponse>.Success(
                new UpdateMenuItemOptionGroupResponse
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

using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupHandler
        : IRequestHandler<CreateOptionGroupCommand, Result<CreateOptionGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateOptionGroupHandler> _logger;
        private readonly IMessageService _messageService;

        public CreateOptionGroupHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateOptionGroupHandler> logger,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<CreateOptionGroupResponse>> Handle(
            CreateOptionGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start creating option group Name={OptionGroupName} LegacyMenuItemId={MenuItemId}",
                request.Name,
                request.MenuItemId
            );

            MenuItem? menuItem = null;
            if (request.MenuItemId.HasValue)
            {
                menuItem = await _unitOfWork
                    .Repository<MenuItem>()
                    .Query()
                    .FirstOrDefaultAsync(
                        x => x.MenuItemId == request.MenuItemId.Value,
                        cancellationToken
                    );

                if (menuItem == null)
                {
                    return Result<CreateOptionGroupResponse>.NotFound(
                        _messageService.GetMessage(MessageKeys.MenuItem.NotFound, request.MenuItemId.Value)
                    );
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var optionGroup = OptionGroup.Create(
                    request.Name,
                    request.Type,
                    request.IsRequired,
                    request.MenuItemId
                );

                await _unitOfWork.Repository<OptionGroup>().AddAsync(optionGroup);

                MenuItemOptionGroup? assignment = null;
                if (menuItem != null)
                {
                    assignment = MenuItemOptionGroup.Create(
                        menuItem.MenuItemId,
                        optionGroup.OptionGroupId,
                        request.Type,
                        request.IsRequired,
                        request.MinSelect,
                        request.MaxSelect,
                        request.SortOrder,
                        request.IsVisible
                    );

                    await _unitOfWork.Repository<MenuItemOptionGroup>().AddAsync(assignment);
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                var response = new CreateOptionGroupResponse
                {
                    OptionGroupId = optionGroup.OptionGroupId,
                    MenuItemId = optionGroup.MenuItemId,
                    Name = optionGroup.Name,
                    Type = (int)optionGroup.OptionType,
                    IsRequired = assignment?.IsRequired ?? optionGroup.IsRequired,
                    MinSelect = assignment?.MinSelect ?? optionGroup.GetDefaultMinSelect(),
                    MaxSelect = assignment?.MaxSelect ?? optionGroup.GetDefaultMaxSelect(),
                    SortOrder = assignment?.SortOrder ?? 0,
                    IsVisible = assignment?.IsVisible ?? true,
                    OptionItems = new List<OptionItemResponse>(),
                };

                _logger.LogInformation(
                    "End creating option group OptionGroupId={OptionGroupId} MenuItemOptionGroupCreated={HasAssignment}",
                    optionGroup.OptionGroupId,
                    assignment != null
                );

                return Result<CreateOptionGroupResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "CreateOptionGroup transaction rolled back");
                throw;
            }
        }
    }
}

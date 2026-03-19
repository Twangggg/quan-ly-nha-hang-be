using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Commands.AssignOptionGroupToMenuItem
{
    public class AssignOptionGroupToMenuItemHandler
        : IRequestHandler<
            AssignOptionGroupToMenuItemCommand,
            Result<AssignOptionGroupToMenuItemResponse>
        >
    {
        private readonly IUnitOfWork _unitOfWork;

        public AssignOptionGroupToMenuItemHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AssignOptionGroupToMenuItemResponse>> Handle(
            AssignOptionGroupToMenuItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var menuItemExists = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .AnyAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);
            if (!menuItemExists)
            {
                throw new NotFoundException($"Menu item with ID {request.MenuItemId} not found.");
            }

            var optionGroup = await _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .FirstOrDefaultAsync(x => x.OptionGroupId == request.OptionGroupId, cancellationToken);
            if (optionGroup == null)
            {
                throw new NotFoundException(
                    $"Option group with ID {request.OptionGroupId} not found."
                );
            }

            var existingAssignment = await _unitOfWork
                .Repository<MenuItemOptionGroup>()
                .Query()
                .FirstOrDefaultAsync(
                    x => x.MenuItemId == request.MenuItemId && x.OptionGroupId == request.OptionGroupId,
                    cancellationToken
                );
            if (existingAssignment != null)
            {
                return Result<AssignOptionGroupToMenuItemResponse>.Failure(
                    "Option group is already assigned to this menu item.",
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

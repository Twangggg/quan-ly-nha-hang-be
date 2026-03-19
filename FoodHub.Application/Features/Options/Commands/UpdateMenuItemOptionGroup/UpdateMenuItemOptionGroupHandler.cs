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

namespace FoodHub.Application.Features.Options.Commands.UpdateMenuItemOptionGroup
{
    public class UpdateMenuItemOptionGroupHandler
        : IRequestHandler<
            UpdateMenuItemOptionGroupCommand,
            Result<UpdateMenuItemOptionGroupResponse>
        >
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMenuItemOptionGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateMenuItemOptionGroupResponse>> Handle(
            UpdateMenuItemOptionGroupCommand request,
            CancellationToken cancellationToken
        )
        {
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
                throw new NotFoundException(
                    $"Menu item option group with ID {request.MenuItemOptionGroupId} not found."
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

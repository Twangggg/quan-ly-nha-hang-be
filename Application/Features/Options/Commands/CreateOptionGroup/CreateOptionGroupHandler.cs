using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupHandler : IRequestHandler<CreateOptionGroupCommand, Result<CreateOptionGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateOptionGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateOptionGroupResponse>> Handle(CreateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();
            var menuItem = await menuItemRepository.GetByIdAsync(request.MenuItemId);
            if (menuItem == null)
            {
                return Result<CreateOptionGroupResponse>.Failure($"Menu item with ID {request.MenuItemId} not found.", ResultErrorType.NotFound);
            }

            var optionGroup = new OptionGroup
            {
                MenuItemId = request.MenuItemId,
                Name = request.Name,
                OptionType = (OptionGroupType)request.Type,
                IsRequired = request.IsRequired
            };

            await _unitOfWork.Repository<OptionGroup>().AddAsync(optionGroup);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new CreateOptionGroupResponse
            {
                OptionGroupId = optionGroup.OptionGroupId,
                MenuItemId = optionGroup.MenuItemId,
                Name = optionGroup.Name,
                Type = (int)optionGroup.OptionType,
                IsRequired = optionGroup.IsRequired,
                OptionItems = new List<OptionItemResponse>()
            };

            return Result<CreateOptionGroupResponse>.Success(response);
        }
    }
}

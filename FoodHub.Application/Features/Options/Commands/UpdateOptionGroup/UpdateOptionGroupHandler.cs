using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupHandler
        : IRequestHandler<UpdateOptionGroupCommand, Result<UpdateOptionGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOptionGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateOptionGroupResponse>> Handle(
            UpdateOptionGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            var optionGroup = await _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .Include(og => og.OptionItems)
                .FirstOrDefaultAsync(
                    og => og.OptionGroupId == request.OptionGroupId,
                    cancellationToken
                );

            if (optionGroup == null)
            {
                return Result<UpdateOptionGroupResponse>.Failure(
                    $"Option group with ID {request.OptionGroupId} not found.",
                    ResultErrorType.NotFound
                );
            }

            optionGroup.Name = request.Name;
            optionGroup.OptionType = (OptionGroupType)request.Type;
            optionGroup.IsRequired = request.IsRequired;

            _unitOfWork.Repository<OptionGroup>().Update(optionGroup);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new UpdateOptionGroupResponse
            {
                OptionGroupId = optionGroup.OptionGroupId,
                MenuItemId = optionGroup.MenuItemId,
                Name = optionGroup.Name,
                Type = (int)optionGroup.OptionType,
                IsRequired = optionGroup.IsRequired,
            };

            return Result<UpdateOptionGroupResponse>.Success(response);
        }
    }
}

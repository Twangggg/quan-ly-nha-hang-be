using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupHandler : IRequestHandler<UpdateOptionGroupCommand, Result<OptionGroupDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOptionGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<OptionGroupDto>> Handle(UpdateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            var optionGroup = await _unitOfWork.Repository<OptionGroup>()
                .Query()
                .Include(og => og.OptionItems)
                .FirstOrDefaultAsync(og => og.OptionGroupId == request.OptionGroupId, cancellationToken);

            if (optionGroup == null)
            {
                return Result<OptionGroupDto>.Failure($"Option group with ID {request.OptionGroupId} not found.", ResultErrorType.NotFound);
            }

            optionGroup.Name = request.Name;
            optionGroup.Type = (OptionGroupType)request.Type;
            optionGroup.IsRequired = request.IsRequired;

            _unitOfWork.Repository<OptionGroup>().Update(optionGroup);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var dto = new OptionGroupDto
            {
                OptionGroupId = optionGroup.OptionGroupId,
                Name = optionGroup.Name,
                Type = (int)optionGroup.Type,
                IsRequired = optionGroup.IsRequired,
                OptionItems = optionGroup.OptionItems.Select(oi => new OptionItemDto
                {
                    OptionItemId = oi.OptionItemId,
                    Label = oi.Label,
                    ExtraPrice = oi.ExtraPrice
                }).ToList()
            };

            return Result<OptionGroupDto>.Success(dto);
        }
    }
}

using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemHandler : IRequestHandler<CreateOptionItemCommand, Result<CreateOptionItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateOptionItemHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateOptionItemResponse>> Handle(CreateOptionItemCommand request, CancellationToken cancellationToken)
        {
            var optionGroupRepository = _unitOfWork.Repository<OptionGroup>();
            var optionGroup = await optionGroupRepository.GetByIdAsync(request.OptionGroupId);
            if (optionGroup == null)
            {
                return Result<CreateOptionItemResponse>.Failure($"Option group with ID {request.OptionGroupId} not found.", ResultErrorType.NotFound);
            }

            var optionItem = new OptionItem
            {
                OptionGroupId = request.OptionGroupId,
                Label = request.Label,
                ExtraPrice = request.ExtraPrice
            };

            await _unitOfWork.Repository<OptionItem>().AddAsync(optionItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new CreateOptionItemResponse
            {
                OptionItemId = optionItem.OptionItemId,
                OptionGroupId = optionItem.OptionGroupId,
                Label = optionItem.Label,
                ExtraPrice = optionItem.ExtraPrice
            };

            return Result<CreateOptionItemResponse>.Success(response);
        }
    }
}

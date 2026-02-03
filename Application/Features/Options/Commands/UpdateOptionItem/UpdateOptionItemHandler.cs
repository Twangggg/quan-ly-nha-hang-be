using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemHandler : IRequestHandler<UpdateOptionItemCommand, Result<UpdateOptionItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOptionItemHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateOptionItemResponse>> Handle(UpdateOptionItemCommand request, CancellationToken cancellationToken)
        {
            var optionItem = await _unitOfWork.Repository<OptionItem>().GetByIdAsync(request.OptionItemId);

            if (optionItem == null)
            {
                return Result<UpdateOptionItemResponse>.Failure($"Option item with ID {request.OptionItemId} not found.", ResultErrorType.NotFound);
            }

            optionItem.Label = request.Label;
            optionItem.ExtraPrice = request.ExtraPrice;

            _unitOfWork.Repository<OptionItem>().Update(optionItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new UpdateOptionItemResponse
            {
                OptionItemId = optionItem.OptionItemId,
                OptionGroupId = optionItem.OptionGroupId,
                Label = optionItem.Label,
                ExtraPrice = optionItem.ExtraPrice
            };

            return Result<UpdateOptionItemResponse>.Success(response);
        }
    }
}

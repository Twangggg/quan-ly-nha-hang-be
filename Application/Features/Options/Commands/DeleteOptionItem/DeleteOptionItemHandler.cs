using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionItem
{
    public class DeleteOptionItemHandler : IRequestHandler<DeleteOptionItemCommand, Result<DeleteOptionItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteOptionItemHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<DeleteOptionItemResponse>> Handle(DeleteOptionItemCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<OptionItem>();

            var optionItem = await repo.Query()
                .FirstOrDefaultAsync(oi => oi.OptionItemId == request.OptionItemId, cancellationToken);

            if (optionItem is null) return Result<DeleteOptionItemResponse>.NotFound("Option item is not found!");

            optionItem.DeletedAt = DateTime.UtcNow;
            optionItem.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync();

            return Result<DeleteOptionItemResponse>.Success(new DeleteOptionItemResponse(optionItem.OptionItemId, optionItem.DeletedAt));
        }
    }
}

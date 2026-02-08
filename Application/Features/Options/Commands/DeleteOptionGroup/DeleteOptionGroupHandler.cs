using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionGroup
{
    public class DeleteOptionGroupHandler : IRequestHandler<DeleteOptionGroupCommand, Result<DeleteOptionGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteOptionGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<DeleteOptionGroupResponse>> Handle(DeleteOptionGroupCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<OptionGroup>();

            var optionGroup = await repo.Query()
                .FirstOrDefaultAsync(og => og.OptionGroupId == request.OptionGroupId, cancellationToken);

            if (optionGroup is null) return Result<DeleteOptionGroupResponse>.NotFound("Option group is not found!");

            optionGroup.DeletedAt = DateTime.UtcNow;
            optionGroup.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync();

            return Result<DeleteOptionGroupResponse>.Success(new DeleteOptionGroupResponse(optionGroup.OptionGroupId, optionGroup.DeletedAt));
        }
    }
}

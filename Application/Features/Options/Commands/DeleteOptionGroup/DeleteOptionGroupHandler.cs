using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionGroup
{
    public class DeleteOptionGroupHandler : IRequestHandler<DeleteOptionGroupCommand, Result<DeleteOptionGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public DeleteOptionGroupHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<DeleteOptionGroupResponse>> Handle(DeleteOptionGroupCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<OptionGroup>();

            var optionGroup = await repo.Query()
                .FirstOrDefaultAsync(og => og.OptionGroupId == request.OptionGroupId, cancellationToken);

            if (optionGroup is null) return Result<DeleteOptionGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.OptionGroup.NotFound));

            optionGroup.DeletedAt = DateTime.UtcNow;
            optionGroup.UpdatedAt = DateTime.UtcNow;
            optionGroup.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            await _unitOfWork.SaveChangeAsync();

            return Result<DeleteOptionGroupResponse>.Success(new DeleteOptionGroupResponse(optionGroup.OptionGroupId, optionGroup.DeletedAt));
        }
    }
}

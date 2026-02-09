using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionItem
{
    public class DeleteOptionItemHandler : IRequestHandler<DeleteOptionItemCommand, Result<DeleteOptionItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public DeleteOptionItemHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<DeleteOptionItemResponse>> Handle(DeleteOptionItemCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<OptionItem>();

            var optionItem = await repo.Query()
                .FirstOrDefaultAsync(oi => oi.OptionItemId == request.OptionItemId, cancellationToken);

            if (optionItem is null) return Result<DeleteOptionItemResponse>.NotFound(_messageService.GetMessage(MessageKeys.OptionItem.NotFound));

            optionItem.DeletedAt = DateTime.UtcNow;
            optionItem.UpdatedAt = DateTime.UtcNow;
            optionItem.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            await _unitOfWork.SaveChangeAsync();

            return Result<DeleteOptionItemResponse>.Success(new DeleteOptionItemResponse(optionItem.OptionItemId, optionItem.DeletedAt));
        }
    }
}

using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Promotions.Commands.UpdatePromotionStatus
{
    public class UpdatePromotionStatusHandler
        : IRequestHandler<UpdatePromotionStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public UpdatePromotionStatusHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(
            UpdatePromotionStatusCommand request,
            CancellationToken cancellationToken
        )
        {
            var promotion = await _unitOfWork
                .Repository<Promotion>()
                .Query()
                .Where(p => p.DeletedAt == null)
                .FirstOrDefaultAsync(p => p.PromotionId == request.PromotionId, cancellationToken);

            if (promotion is null)
            {
                return Result<bool>.NotFound(
                    _messageService.GetMessage(MessageKeys.Voucher.NotFound)
                );
            }

            promotion.IsActive = request.IsActive;
            promotion.UpdatedAt = DateTime.UtcNow;
            promotion.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId)
                ? userId
                : (Guid?)null;

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

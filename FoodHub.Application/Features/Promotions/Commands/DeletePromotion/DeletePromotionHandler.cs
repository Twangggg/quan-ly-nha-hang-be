using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Promotions.Commands.DeletePromotion
{
    public class DeletePromotionHandler
        : IRequestHandler<DeletePromotionCommand, Result<DeletePromotionResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public DeletePromotionHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<DeletePromotionResponse>> Handle(
            DeletePromotionCommand request,
            CancellationToken cancellationToken
        )
        {
            var promotionRepo = _unitOfWork.Repository<Promotion>();

            var promotion = await promotionRepo
                .Query()
                .Where(p => p.DeletedAt == null)
                .FirstOrDefaultAsync(p => p.PromotionId == request.PromotionId, cancellationToken);

            if (promotion is null)
            {
                return Result<DeletePromotionResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Voucher.NotFound)
                );
            }

            Guid? userId = Guid.TryParse(_currentUserService.UserId, out var parsedUserId)
                ? parsedUserId
                : null;

            promotion.MarkDeleted(userId);
            promotionRepo.Update(promotion);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<DeletePromotionResponse>.Success(
                new DeletePromotionResponse(promotion.PromotionId, promotion.DeletedAt)
            );
        }
    }
}

using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Promotions.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Promotions.Commands.UpdatePromotion
{
    public class UpdatePromotionHandler
        : IRequestHandler<UpdatePromotionCommand, Result<PromotionResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public UpdatePromotionHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PromotionResponse>> Handle(
            UpdatePromotionCommand request,
            CancellationToken cancellationToken
        )
        {
            var promotionRepo = _unitOfWork.Repository<Promotion>();

            var promotion = await promotionRepo
                .Query()
                .Where(p => p.DeletedAt == null)
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.PromotionId == request.PromotionId, cancellationToken);

            if (promotion is null)
            {
                return Result<PromotionResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Voucher.NotFound)
                );
            }

            var codeExists = await promotionRepo.AnyAsync(p =>
                p.Code == request.Code && p.PromotionId != request.PromotionId
            );
            if (codeExists)
            {
                return Result<PromotionResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Voucher.CodeAlreadyExists),
                    ResultErrorType.Conflict
                );
            }

            MenuItem? item = null;
            if (request.Type == PromotionType.FreeItem && request.ItemId.HasValue)
            {
                item = await _unitOfWork
                    .Repository<MenuItem>()
                    .Query()
                    .FirstOrDefaultAsync(mi => mi.MenuItemId == request.ItemId.Value, cancellationToken);

                if (item is null)
                {
                    return Result<PromotionResponse>.NotFound(
                        _messageService.GetMessage(MessageKeys.MenuItem.NotFound)
                    );
                }
            }

            Guid? userId = Guid.TryParse(_currentUserService.UserId, out var parsedUserId)
                ? parsedUserId
                : null;

            promotion.Code = request.Code.Trim();
            promotion.Type = request.Type;
            promotion.Value = request.Value;
            promotion.MaxDiscount = request.MaxDiscount;
            promotion.MinOrderValue = request.MinOrderValue;
            promotion.ItemId = request.Type == PromotionType.FreeItem ? request.ItemId : null;
            promotion.Item = item;
            promotion.FreeQuantity = request.Type == PromotionType.FreeItem ? request.FreeQuantity : null;
            promotion.StartDate = request.StartDate;
            promotion.EndDate = request.EndDate;
            promotion.StartTime = request.StartTime;
            promotion.EndTime = request.EndTime;
            promotion.IsActive = request.IsActive;
            promotion.UsageLimit = request.UsageLimit;
            promotion.UpdatedAt = DateTime.UtcNow;
            promotion.UpdatedBy = userId;

            promotionRepo.Update(promotion);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<PromotionResponse>.Success(_mapper.Map<PromotionResponse>(promotion));
        }
    }
}

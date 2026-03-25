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

namespace FoodHub.Application.Features.Promotions.Commands.CreatePromotion
{
    public class CreatePromotionHandler
        : IRequestHandler<CreatePromotionCommand, Result<PromotionResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public CreatePromotionHandler(
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
            CreatePromotionCommand request,
            CancellationToken cancellationToken
        )
        {
            var promotionRepo = _unitOfWork.Repository<Promotion>();

            var codeExists = await promotionRepo.AnyAsync(p => p.Code == request.Code);
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

            var promotion = new Promotion
            {
                PromotionId = Guid.NewGuid(),
                Code = request.Code.Trim(),
                Type = request.Type,
                Value = request.Value,
                MaxDiscount = request.MaxDiscount,
                MinOrderValue = request.MinOrderValue,
                ItemId = request.Type == PromotionType.FreeItem ? request.ItemId : null,
                Item = item,
                FreeQuantity = request.Type == PromotionType.FreeItem ? request.FreeQuantity : null,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsActive = request.IsActive,
                UsageLimit = request.UsageLimit,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedBy = userId,
            };

            await promotionRepo.AddAsync(promotion);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<PromotionResponse>.Success(_mapper.Map<PromotionResponse>(promotion));
        }
    }
}

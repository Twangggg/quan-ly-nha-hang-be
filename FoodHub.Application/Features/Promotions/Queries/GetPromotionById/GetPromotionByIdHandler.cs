using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Promotions.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Promotions.Queries.GetPromotionById
{
    public class GetPromotionByIdHandler
        : IRequestHandler<GetPromotionByIdQuery, Result<PromotionResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public GetPromotionByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<PromotionResponse>> Handle(
            GetPromotionByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var promotion = await _unitOfWork
                .Repository<Promotion>()
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

            return Result<PromotionResponse>.Success(_mapper.Map<PromotionResponse>(promotion));
        }
    }
}

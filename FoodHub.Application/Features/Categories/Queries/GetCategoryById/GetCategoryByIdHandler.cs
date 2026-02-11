using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, Result<GetCategoryByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public GetCategoryByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<GetCategoryByIdResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.CategoryById, request.CategoryId);

            var cachedCategory = await _cacheService.GetAsync<GetCategoryByIdResponse>(cacheKey, cancellationToken);
            if (cachedCategory != null)
            {
                return Result<GetCategoryByIdResponse>.Success(cachedCategory);
            }

            var category = await _unitOfWork.Repository<Category>()
                .GetByIdAsync(request.CategoryId);

            if (category == null)
            {
                return Result<GetCategoryByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Category.NotFound));
            }

            var response = _mapper.Map<GetCategoryByIdResponse>(category);

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Categories, cancellationToken);

            return Result<GetCategoryByIdResponse>.Success(response);
        }
    }
}

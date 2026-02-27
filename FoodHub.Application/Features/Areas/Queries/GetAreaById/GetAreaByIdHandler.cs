using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Areas.Queries.GetAreaById
{
    public class GetAreaByIdHandler : IRequestHandler<GetAreaByIdQuery, Result<GetAreaByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public GetAreaByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<GetAreaByIdResponse>> Handle(GetAreaByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.AreaById, request.AreaId);

            var cachedArea = await _cacheService.GetAsync<GetAreaByIdResponse>(cacheKey, cancellationToken);
            if (cachedArea != null)
            {
                return Result<GetAreaByIdResponse>.Success(cachedArea);
            }

            var area = await _unitOfWork.Repository<Area>()
                .GetByIdAsync(request.AreaId);

            if (area == null)
            {
                return Result<GetAreaByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Area.NotFound));
            }

            var response = _mapper.Map<GetAreaByIdResponse>(area);

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Areas, cancellationToken);

            return Result<GetAreaByIdResponse>.Success(response);
        }
    }
}

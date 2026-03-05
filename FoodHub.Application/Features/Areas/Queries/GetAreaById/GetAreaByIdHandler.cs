using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Areas.Queries.GetAreaById
{
    public class GetAreaByIdHandler : IRequestHandler<GetAreaByIdQuery, Result<GetAreaByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetAreaByIdHandler> _logger;

        public GetAreaByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<GetAreaByIdHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetAreaByIdResponse>> Handle(
            GetAreaByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Bắt đầu lấy thông tin khu vực. AreaId: {AreaId}",
                request.AreaId
            );

            var cacheKey = string.Format(CacheKey.AreaById, request.AreaId);

            var cachedArea = await _cacheService.GetAsync<GetAreaByIdResponse>(
                cacheKey,
                cancellationToken
            );
            if (cachedArea != null)
            {
                _logger.LogInformation(
                    "Hoàn tất lấy thông tin khu vực (từ Cache). AreaId: {AreaId}",
                    request.AreaId
                );
                return Result<GetAreaByIdResponse>.Success(cachedArea);
            }

            var area = await _unitOfWork
                .Repository<Area>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId, cancellationToken);

            if (area == null)
            {
                _logger.LogWarning("Khu vực không tồn tại. AreaId: {AreaId}", request.AreaId);
                return Result<GetAreaByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Area.NotFound)
                );
            }

            var response = _mapper.Map<GetAreaByIdResponse>(area);

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Areas, cancellationToken);

            _logger.LogInformation(
                "Hoàn tất lấy thông tin khu vực (từ Database). AreaId: {AreaId}",
                request.AreaId
            );
            return Result<GetAreaByIdResponse>.Success(response);
        }
    }
}

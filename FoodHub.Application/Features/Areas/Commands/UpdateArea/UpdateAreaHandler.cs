using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Areas.Commands.UpdateArea
{
    public class UpdateAreaHandler : IRequestHandler<UpdateAreaCommand, Result<GetAreaByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateAreaHandler> _logger;

        public UpdateAreaHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<UpdateAreaHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetAreaByIdResponse>> Handle(
            UpdateAreaCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Bắt đầu cập nhật khu vực. AreaId: {AreaId}", request.AreaId);

            var area = await _unitOfWork
                .Repository<Area>()
                .Query()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId, cancellationToken);

            if (area is null)
            {
                _logger.LogWarning(
                    "Cập nhật thất bại. Khu vực không tồn tại. AreaId: {AreaId}",
                    request.AreaId
                );
                return Result<GetAreaByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Area.NotFound)
                );
            }

            // Chỉ cho sửa Name, Description, Type — không cho sửa CodePrefix
            area.Update(request.Name, request.Description, request.Type);

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveAsync(
                string.Format(CacheKey.AreaById, request.AreaId),
                cancellationToken
            );

            var response = _mapper.Map<GetAreaByIdResponse>(area);

            _logger.LogInformation("Cập nhật khu vực thành công. AreaId: {AreaId}", request.AreaId);
            return Result<GetAreaByIdResponse>.Success(response);
        }
    }
}

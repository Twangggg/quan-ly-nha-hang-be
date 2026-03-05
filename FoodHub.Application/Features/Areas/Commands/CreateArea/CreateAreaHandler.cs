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

namespace FoodHub.Application.Features.Areas.Commands.CreateArea
{
    public class CreateAreaHandler : IRequestHandler<CreateAreaCommand, Result<GetAreaByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateAreaHandler> _logger;

        public CreateAreaHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<CreateAreaHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetAreaByIdResponse>> Handle(
            CreateAreaCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Bắt đầu tạo khu vực mới. CodePrefix: {CodePrefix}",
                request.CodePrefix
            );

            var existingArea = await _unitOfWork
                .Repository<Area>()
                .Query()
                .FirstOrDefaultAsync(a => a.CodePrefix == request.CodePrefix, cancellationToken);

            if (existingArea != null)
            {
                _logger.LogWarning(
                    "Tạo khu vực thất bại. CodePrefix '{CodePrefix}' đã tồn tại.",
                    request.CodePrefix
                );
                return Result<GetAreaByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Area.CodeExists)
                );
            }

            var area = new Area
            {
                Name = request.Name,
                CodePrefix = request.CodePrefix,
                Type = request.Type,
                Description = request.Description,
            };

            await _unitOfWork.Repository<Area>().AddAsync(area);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var result = _mapper.Map<GetAreaByIdResponse>(area);

            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);

            _logger.LogInformation(
                "Tạo khu vực thành công. AreaId: {AreaId}, CodePrefix: {CodePrefix}",
                area.AreaId,
                area.CodePrefix
            );
            return Result<GetAreaByIdResponse>.Success(result);
        }
    }
}

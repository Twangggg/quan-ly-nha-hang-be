using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Areas.Commands.UpdateArea
{
    public class UpdateAreaHandler : IRequestHandler<UpdateAreaCommand, Result<GetAreaByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public UpdateAreaHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<GetAreaByIdResponse>> Handle(
            UpdateAreaCommand request,
            CancellationToken cancellationToken
        )
        {
            var area = await _unitOfWork
                .Repository<Area>()
                .Query()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId, cancellationToken);

            if (area is null)
                return Result<GetAreaByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Area.NotFound)
                );

            var domainResult = area.UpdateDetails(
                request.Name,
                request.CodePrefix,
                request.Description,
                request.Type
            );

            if (!domainResult.IsSuccess)
                return Result<GetAreaByIdResponse>.Failure(MapDomainError(domainResult.ErrorCode));

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveAsync(
                string.Format(CacheKey.AreaById, request.AreaId),
                cancellationToken
            );

            var response = _mapper.Map<GetAreaByIdResponse>(area);
            return Result<GetAreaByIdResponse>.Success(response);
        }

        private string MapDomainError(string? errorCode) =>
            errorCode switch
            {
                DomainErrors.Area.AlreadyInactive => _messageService.GetMessage(
                    MessageKeys.Area.DeactivateForbidden
                ),
                _ => _messageService.GetMessage(MessageKeys.Area.UpdateForbidden),
            };
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Areas.Commands.UpdateAreaStatus
{
    public class UpdateAreaStatusHandler : IRequestHandler<UpdateAreaStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateAreaStatusHandler> _logger;

        public UpdateAreaStatusHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpdateAreaStatusHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            UpdateAreaStatusCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start updating area status. AreaId: {AreaId}, IsActive: {IsActive}",
                request.AreaId,
                request.IsActive
            );

            var repo = _unitOfWork.Repository<Area>();
            var area = await repo.Query()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId, cancellationToken);

            if (area == null)
            {
                _logger.LogWarning(
                    "Failed to update area status because the area was not found. AreaId: {AreaId}",
                    request.AreaId
                );
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Area.NotFound),
                    ResultErrorType.NotFound
                );
            }

            Guid? auditorId = Guid.TryParse(_currentUserService.UserId, out var uid) ? uid : null;
            var domainResult = area.UpdateStatus(request.IsActive, auditorId);

            if (!domainResult.IsSuccess)
            {
                return Result<bool>.Failure(MapDomainError(domainResult.ErrorCode));
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveAsync(
                string.Format(CacheKey.AreaById, request.AreaId),
                cancellationToken
            );

            _logger.LogInformation(
                "Updated area status successfully. AreaId: {AreaId}, IsActive: {IsActive}",
                request.AreaId,
                request.IsActive
            );
            return Result<bool>.Success(true);
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

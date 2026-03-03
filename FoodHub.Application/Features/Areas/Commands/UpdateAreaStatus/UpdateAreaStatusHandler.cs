using System;
using System.Threading;
using System.Threading.Tasks;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Areas.Commands.UpdateAreaStatus
{
    public class UpdateAreaStatusHandler : IRequestHandler<UpdateAreaStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public UpdateAreaStatusHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(UpdateAreaStatusCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Area>();
            var area = await repo.Query()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId, cancellationToken);

            if (area == null)
            {
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Area.NotFound),
                    ResultErrorType.NotFound);
            }

            area.Status = request.IsActive ? AreaStatus.Active : AreaStatus.Inactive;
            area.UpdatedAt = DateTime.UtcNow;

            if (Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                area.UpdatedBy = userId;
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.AreaById, request.AreaId), cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

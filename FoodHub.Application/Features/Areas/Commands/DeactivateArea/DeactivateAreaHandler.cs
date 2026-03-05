using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Areas.Commands.DeactivateArea
{
    public class DeactivateAreaHandler : IRequestHandler<DeactivateAreaCommand, Result<DeactivateAreaResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public DeactivateAreaHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<DeactivateAreaResponse>> Handle(DeactivateAreaCommand request, CancellationToken cancellationToken)
        {
            var area = await _unitOfWork.Repository<Area>()
                .Query()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId, cancellationToken);

            if (area is null)
                return Result<DeactivateAreaResponse>.NotFound(_messageService.GetMessage(MessageKeys.Area.NotFound));

            if (area.Status == AreaStatus.Inactive)
                return Result<DeactivateAreaResponse>.Failure(_messageService.GetMessage(MessageKeys.Area.DeactivateForbidden));

            area.Status = AreaStatus.Inactive;
            area.UpdatedAt = DateTime.UtcNow;
            area.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.AreaById, request.AreaId), cancellationToken);

            return Result<DeactivateAreaResponse>.Success(
                new DeactivateAreaResponse(area.AreaId, area.Status, area.UpdatedAt));
        }
    }
}

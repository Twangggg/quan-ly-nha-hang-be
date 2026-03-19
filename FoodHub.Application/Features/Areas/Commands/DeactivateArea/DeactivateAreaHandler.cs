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

namespace FoodHub.Application.Features.Areas.Commands.DeactivateArea
{
    public class DeactivateAreaHandler
        : IRequestHandler<DeactivateAreaCommand, Result<DeactivateAreaResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public DeactivateAreaHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<DeactivateAreaResponse>> Handle(
            DeactivateAreaCommand request,
            CancellationToken cancellationToken
        )
        {
            var area = await _unitOfWork
                .Repository<Area>()
                .Query()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId, cancellationToken);

            if (area is null)
                return Result<DeactivateAreaResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Area.NotFound)
                );

            Guid? updatedBy = Guid.TryParse(_currentUserService.UserId, out var userId)
                ? userId
                : null;
            var domainResult = area.Deactivate(updatedBy);

            if (!domainResult.IsSuccess)
                return Result<DeactivateAreaResponse>.Failure(MapDomainError(domainResult.ErrorCode));

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveAsync(
                string.Format(CacheKey.AreaById, request.AreaId),
                cancellationToken
            );

            return Result<DeactivateAreaResponse>.Success(
                new DeactivateAreaResponse(area.AreaId, area.Status, area.UpdatedAt)
            );
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

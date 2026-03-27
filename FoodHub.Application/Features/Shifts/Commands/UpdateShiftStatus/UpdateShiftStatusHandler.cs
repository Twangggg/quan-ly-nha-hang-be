using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus
{
    public class UpdateShiftStatusHandler : IRequestHandler<UpdateShiftStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public UpdateShiftStatusHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(
            UpdateShiftStatusCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser),
                    ResultErrorType.Unauthorized
                );
            }

            var shift = await _unitOfWork.Repository<Shift>().Query()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null)
                return Result<bool>.NotFound(_messageService.GetMessage(MessageKeys.Shift.NotFound));

            var domainResult = shift.UpdateStatus(request.IsActive, auditorId);
            if (!domainResult.IsSuccess)
                return Result<bool>.Failure(_messageService.GetMessage(domainResult.ErrorCode ?? MessageKeys.Common.InternalServerError));

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(CacheKey.ShiftList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.ShiftById, request.ShiftId), cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

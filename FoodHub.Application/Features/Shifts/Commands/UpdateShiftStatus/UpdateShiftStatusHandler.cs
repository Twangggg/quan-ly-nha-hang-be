using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus
{
    public class UpdateShiftStatusHandler : IRequestHandler<UpdateShiftStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateShiftStatusHandler> _logger;

        public UpdateShiftStatusHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<UpdateShiftStatusHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            UpdateShiftStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating shift status: {Id} to IsActive={IsActive}", request.ShiftId, request.IsActive);

             if (_currentUserService.Role is not "Manager" && _currentUserService.Role is not "Admin")
             {
                 return Result<bool>.Failure(
                    "Forbidden",
                    ResultErrorType.Forbidden);
             }

            var repo = _unitOfWork.Repository<Shift>();
            var shift = await repo.Query()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null)
            {
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Shift.NotFound),
                    ResultErrorType.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Guid? auditorId = Guid.TryParse(_currentUserService.UserId, out var uid) ? uid : null;
                
                // Toggle status via boolean as expected by the command pattern
                var domainResult = request.IsActive ? shift.Activate(auditorId) : shift.Deactivate(auditorId);

                if (!domainResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<bool>.Failure(domainResult.ErrorCode ?? "Error in domain logic");
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemoveAsync(CacheKey.ShiftList, cancellationToken);
                await _cacheService.RemoveAsync(
                    string.Format(CacheKey.ShiftById, shift.ShiftId), cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                 await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating shift status: {Message}", ex.Message);
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.BadRequest);
            }
        }
    }
}

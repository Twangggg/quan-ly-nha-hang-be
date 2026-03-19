using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShift
{
    public class UpdateShiftHandler : IRequestHandler<UpdateShiftCommand, Result<UpdateShiftResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UpdateShiftHandler> _logger;

        public UpdateShiftHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<UpdateShiftHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<UpdateShiftResponse>> Handle(
            UpdateShiftCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating shift: {Id}", request.ShiftId);

             if (_currentUserService.Role is not "Manager" && _currentUserService.Role is not "Admin")
             {
                 return Result<UpdateShiftResponse>.Failure(
                    "You don't have permission to perform this action.",
                    ResultErrorType.Forbidden);
             }

            var repo = _unitOfWork.Repository<Shift>();
            var shift = await repo.Query()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null)
            {
                return Result<UpdateShiftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Shift.NotFound),
                    ResultErrorType.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Guid? auditorId = Guid.TryParse(_currentUserService.UserId, out var uid) ? uid : null;
                var domainResult = shift.Update(request.Name, request.StartTime, request.EndTime, auditorId);

                if (!domainResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<UpdateShiftResponse>.Failure(domainResult.ErrorCode ?? "Error in domain logic");
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemoveAsync(CacheKey.ShiftList, cancellationToken);
                await _cacheService.RemoveAsync(
                    string.Format(CacheKey.ShiftById, shift.ShiftId), cancellationToken);

                var response = new UpdateShiftResponse
                {
                    ShiftId = shift.ShiftId,
                    Name = shift.Name,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    Status = shift.Status,
                    UpdatedAt = shift.UpdatedAt
                };

                return Result<UpdateShiftResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating shift: {Message}", ex.Message);
                return Result<UpdateShiftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.BadRequest);
            }
        }
    }
}

using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Shifts.Commands.CreateShift
{
    public class CreateShiftHandler : IRequestHandler<CreateShiftCommand, Result<CreateShiftResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CreateShiftHandler> _logger;

        public CreateShiftHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<CreateShiftHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<CreateShiftResponse>> Handle(
            CreateShiftCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating shift: {Name}", request.Name);

            if (_currentUserService.Role is not "Manager" && _currentUserService.Role is not "Admin")
            {
                 return Result<CreateShiftResponse>.Failure(
                    "You don't have permission to perform this action.", // Standard message if applicable
                    ResultErrorType.Forbidden);
            }

            var repo = _unitOfWork.Repository<Shift>();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Guid? createdBy = Guid.TryParse(_currentUserService.UserId, out var uid) ? uid : null;

                var shift = Shift.Create(
                    request.Name,
                    request.StartTime,
                    request.EndTime,
                    createdBy);

                await repo.AddAsync(shift);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemoveAsync(CacheKey.ShiftList, cancellationToken);

                var response = new CreateShiftResponse
                {
                    ShiftId = shift.ShiftId,
                    Name = shift.Name,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    Status = shift.Status,
                    CreatedAt = shift.CreatedAt
                };

                return Result<CreateShiftResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error occurred while creating shift: {Message}", ex.Message);
                return Result<CreateShiftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.BadRequest);
            }
        }
    }
}

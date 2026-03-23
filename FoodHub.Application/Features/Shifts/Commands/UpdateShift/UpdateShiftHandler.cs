using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShift
{
    public class UpdateShiftHandler : IRequestHandler<UpdateShiftCommand, Result<UpdateShiftResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateShiftHandler> _logger;

        public UpdateShiftHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpdateShiftHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<UpdateShiftResponse>> Handle(
            UpdateShiftCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
                return Result<UpdateShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);

            var shift = await _unitOfWork.Repository<Shift>().Query()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null)
                return Result<UpdateShiftResponse>.NotFound(_messageService.GetMessage(MessageKeys.Shift.NotFound));

            shift.UpdateDetails(request.Name, request.StartTime, request.EndTime, auditorId);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _cacheService.RemoveByPatternAsync(CacheKey.ShiftList, cancellationToken);
                await _cacheService.RemoveAsync(string.Format(CacheKey.ShiftById, request.ShiftId), cancellationToken);

                return Result<UpdateShiftResponse>.Success(_mapper.Map<UpdateShiftResponse>(shift));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "UpdateShift failed for ID {Id}", request.ShiftId);
                var inner = ex.InnerException?.Message ?? ex.Message;
                if (inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || inner.Contains("unique", StringComparison.OrdinalIgnoreCase))
                    return Result<UpdateShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseConflict), ResultErrorType.Conflict);

                throw;
            }
        }
    }
}

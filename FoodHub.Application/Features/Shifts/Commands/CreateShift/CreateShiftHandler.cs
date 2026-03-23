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

namespace FoodHub.Application.Features.Shifts.Commands.CreateShift
{
    public class CreateShiftHandler : IRequestHandler<CreateShiftCommand, Result<CreateShiftResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateShiftHandler> _logger;

        public CreateShiftHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<CreateShiftHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<CreateShiftResponse>> Handle(
            CreateShiftCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<CreateShiftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser),
                    ResultErrorType.Unauthorized
                );
            }

            var shift = Shift.Create(request.Name, request.StartTime, request.EndTime, auditorId);

            try
            {
                await _unitOfWork.Repository<Shift>().AddAsync(shift);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(CacheKey.ShiftList, cancellationToken);

                return Result<CreateShiftResponse>.Success(_mapper.Map<CreateShiftResponse>(shift));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "CreateShift failed for {Name}", request.Name);
                var inner = ex.InnerException?.Message ?? ex.Message;
                if (inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || inner.Contains("unique", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<CreateShiftResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.DatabaseConflict),
                        ResultErrorType.Conflict
                    );
                }
                throw;
            }
        }
    }
}
